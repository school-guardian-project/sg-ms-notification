using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

/// <summary>
/// FR-8.1 (the driver reports an incident or emergency during the trip) and
/// FR-8.2 (the school and the guardians of the students on board are notified).
/// These tests pin down what the report persists, how recipients are resolved and
/// every reason a report is refused.
/// </summary>
[TestFixture]
public class ReportIncidentServiceTests
{
    private const byte AccidentTypeId = 21;

    private readonly Guid _driverId = Guid.NewGuid();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Guid _studentA = Guid.NewGuid();
    private readonly Guid _studentB = Guid.NewGuid();
    private readonly Guid _schoolAdmin = Guid.NewGuid();
    private readonly Guid _parentA = Guid.NewGuid();

    private NotificationContext _context = null!;
    private RecordingEventPublisher _publisher = null!;
    private FakeRouteDirectory _routes = null!;
    private FakeFamilyDirectory _family = null!;
    private FakeSchoolDirectory _school = null!;
    private ReportIncidentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationContext(options);
        _context.AlertTypes.Add(new AlertType
        {
            Id = AccidentTypeId,
            Name = "Accident",
            Description = "Traffic accident involving the bus",
            UrgencyLevel = 5
        });
        _context.SaveChanges();

        _publisher = new RecordingEventPublisher();
        _routes = new FakeRouteDirectory
        {
            CurrentTrip = new CurrentTrip(_tripId, Guid.NewGuid(), Guid.NewGuid(), "InProgress")
        };
        _family = new FakeFamilyDirectory();
        _school = new FakeSchoolDirectory { SchoolAdminProfileIds = new[] { _schoolAdmin } };

        _service = new ReportIncidentService(
            _context, _publisher, _routes, _family, _school,
            NullLogger<ReportIncidentService>.Instance);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private void Board(Guid studentId, BoardingType type, DateTime when)
    {
        _context.Boardings.Add(new Boarding
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = _tripId,
            ProfileId = studentId,
            RouteStopId = Guid.NewGuid(),
            DateTime = when,
            BoardingType = type
        });

        // The service queries the store, so the boarding has to be persisted.
        _context.SaveChanges();
    }

    private static ReportIncidentRequestDto Request(string? description = "Choque leve en la Av. 68") =>
        new() { AlertTypeId = AccidentTypeId, Description = description };

    [Test]
    public async Task Report_RecordsTheAlertForTheTripWithTheDriverAsReporter()
    {
        await _service.ExecuteAsync(_driverId, Request());

        var alert = await _context.Alerts.SingleAsync();
        Assert.That(alert.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(alert.ProfileId, Is.EqualTo(_driverId));
        Assert.That(alert.AlertTypeId, Is.EqualTo(AccidentTypeId));
        Assert.That(alert.Description, Is.EqualTo("Choque leve en la Av. 68"));
    }

    [Test]
    public async Task Report_WithoutDescription_IsAccepted()
    {
        var response = await _service.ExecuteAsync(_driverId, Request(null));

        var alert = await _context.Alerts.SingleAsync();
        Assert.That(alert.Id, Is.EqualTo(response.AlertId));
        Assert.That(alert.Description, Is.Null);
    }

    [Test]
    public async Task Report_NotifiesTheSchoolAndTheGuardiansOfTheStudentsOnBoard()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        _family.ByStudent[_studentA] = new[] { _parentA };

        var response = await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _schoolAdmin, _parentA }));
        Assert.That(response.RecipientsNotified, Is.EqualTo(2));
    }

    [Test]
    public async Task Report_SkipsStudentsWhoAlreadyGotOffTheBus()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        Board(_studentB, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-9));
        Board(_studentB, BoardingType.OFF_BOARD, DateTime.UtcNow.AddMinutes(-2));
        _family.ParentProfileIds = new[] { _parentA };

        var response = await _service.ExecuteAsync(_driverId, Request());

        Assert.That(response.StudentsOnBoard, Is.EqualTo(1));
        Assert.That(_family.Queried, Is.EquivalentTo(new[] { _studentA }));
    }

    [Test]
    public async Task Report_SendsOneNotificationPerPersonEvenWithTwoChildrenOnBoard()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        Board(_studentB, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-9));
        _family.ParentProfileIds = new[] { _parentA };

        await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _schoolAdmin, _parentA }));
    }

    [Test]
    public async Task Report_NeverNotifiesTheDriverReportingIt()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        _school.SchoolAdminProfileIds = new[] { _driverId };
        _family.ParentProfileIds = new[] { _parentA };

        await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _parentA }));
    }

    [Test]
    public async Task Report_PublishesTheIncidentEventWithItsRecipients()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        _family.ParentProfileIds = new[] { _parentA };

        var response = await _service.ExecuteAsync(_driverId, Request());

        var published = _publisher.Published.Single();
        Assert.That(published.Topic, Is.EqualTo(ReportIncidentService.IncidentReportedTopic));

        var @event = (IncidentReportedEvent)published.Payload;
        Assert.That(@event.AlertId, Is.EqualTo(response.AlertId));
        Assert.That(@event.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(@event.ReportedByProfileId, Is.EqualTo(_driverId));
        Assert.That(@event.AlertTypeId, Is.EqualTo(AccidentTypeId));
        Assert.That(@event.StudentsOnBoard, Is.EquivalentTo(new[] { _studentA }));
        Assert.That(@event.Recipients, Is.EquivalentTo(new[] { _schoolAdmin, _parentA }));
    }

    [Test]
    public void Report_WithoutAnActiveTrip_IsRejected()
    {
        _routes.CurrentTrip = null;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteAsync(_driverId, Request()));

        Assert.That(ex!.Message, Does.Contain("no active trip"));
    }

    [Test]
    public async Task Report_WithoutAnActiveTrip_PersistsNothing()
    {
        _routes.CurrentTrip = null;

        try { await _service.ExecuteAsync(_driverId, Request()); } catch (InvalidOperationException) { }

        Assert.That(await _context.Alerts.CountAsync(), Is.Zero);
        Assert.That(_publisher.Published, Is.Empty);
    }

    [Test]
    public void Report_WithATypeOutsideTheCatalog_IsRejected()
    {
        var request = new ReportIncidentRequestDto { AlertTypeId = 200, Description = "x" };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteAsync(_driverId, request));

        Assert.That(ex!.Message, Does.Contain("Unknown incident type"));
    }

    [Test]
    public void Report_WithADescriptionOverTheColumnLimit_IsRejected()
    {
        var tooLong = new string('a', ReportIncidentService.MaxDescriptionLength + 1);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteAsync(_driverId, Request(tooLong)));

        Assert.That(ex!.Message, Does.Contain("too long"));
    }

    [Test]
    public async Task Report_AcceptsADescriptionAtTheColumnLimit()
    {
        var atLimit = new string('a', ReportIncidentService.MaxDescriptionLength);

        var response = await _service.ExecuteAsync(_driverId, Request(atLimit));

        var alert = await _context.Alerts.SingleAsync(a => a.Id == response.AlertId);
        Assert.That(alert.Description, Has.Length.EqualTo(ReportIncidentService.MaxDescriptionLength));
    }

    [Test]
    public async Task Report_StillNotifiesTheGuardiansWhenTheSchoolDirectoryIsDown()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        _family.ParentProfileIds = new[] { _parentA };
        _school.ThrowOnRead = new InvalidOperationException("ms-school-management down");

        var response = await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _parentA }));
        Assert.That(response.RecipientsNotified, Is.EqualTo(1));
    }

    [Test]
    public async Task Report_KeepsNotifyingTheOtherFamiliesWhenOneLookupFails()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        Board(_studentB, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-9));
        _family.ByStudent[_studentA] = new[] { _parentA };
        _family.ThrowByStudent[_studentB] = new InvalidOperationException("ms-user-management down");

        // The lookup for student B blows up, but A's guardian still gets the alert.
        var response = await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _schoolAdmin, _parentA }));
        Assert.That(response.StudentsOnBoard, Is.EqualTo(2));
        Assert.That(response.RecipientsNotified, Is.EqualTo(2));
    }

    [Test]
    public async Task Report_StillRecordsTheIncidentWhenTheFamilyDirectoryIsDown()
    {
        Board(_studentA, BoardingType.ON_BOARD, DateTime.UtcNow.AddMinutes(-10));
        _family.ThrowOnRead = new InvalidOperationException("ms-user-management down");

        var response = await _service.ExecuteAsync(_driverId, Request());

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _schoolAdmin }));
        Assert.That(response.RecipientsNotified, Is.EqualTo(1));
        Assert.That(await _context.Alerts.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Report_WithNobodyOnBoard_StillRecordsTheIncidentForTheSchool()
    {
        var response = await _service.ExecuteAsync(_driverId, Request());

        Assert.That(response.StudentsOnBoard, Is.Zero);
        Assert.That(response.RecipientsNotified, Is.EqualTo(1));

        var recipients = await _context.AlertRecipients.Select(r => r.ProfileId).ToListAsync();
        Assert.That(recipients, Is.EquivalentTo(new[] { _schoolAdmin }));
    }
}
