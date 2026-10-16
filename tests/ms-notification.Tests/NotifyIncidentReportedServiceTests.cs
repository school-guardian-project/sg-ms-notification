using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

/// <summary>
/// FR-8.2, segundo tramo: la entrega por push del incidente que reporto el
/// conductor. Fija a quien se empuja (los destinatarios del evento, via sus
/// dispositivos), con que texto, y que la ausencia de dispositivos no rompe nada
/// (la alerta ya esta en la base y se lee en la app).
/// </summary>
[TestFixture]
public class NotifyIncidentReportedServiceTests
{
    private readonly Guid _driverId = Guid.NewGuid();
    private readonly Guid _schoolAdmin = Guid.NewGuid();
    private readonly Guid _parentA = Guid.NewGuid();
    private readonly Guid _studentA = Guid.NewGuid();
    private const string Description = "Choque en la Av 5";

    private NotificationContext _context = null!;
    private RecordingPushNotifier _push = null!;
    private NotifyIncidentReportedService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationContext(options);
        _push = new RecordingPushNotifier();
        _service = new NotifyIncidentReportedService(
            _context, _push, NullLogger<NotifyIncidentReportedService>.Instance);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private void AddDevice(Guid profileId, string token) =>
        _context.DeviceTokens.Add(new DeviceToken
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            ExpoToken = token,
            UpdatedAt = DateTime.UtcNow
        });

    private static IncidentReportedEvent Incident(string? description, params Guid[] recipients) => new()
    {
        AlertId = Guid.NewGuid(),
        RouteExecutionId = Guid.NewGuid(),
        ReportedByProfileId = Guid.NewGuid(),
        AlertTypeId = 21,
        Description = description,
        StudentsOnBoard = new List<Guid>(),
        Recipients = recipients.ToList(),
        Timestamp = DateTime.UtcNow
    };

    [Test]
    public async Task Pushes_once_to_the_devices_of_every_recipient()
    {
        AddDevice(_schoolAdmin, "ExponentPushToken[school]");
        AddDevice(_parentA, "ExponentPushToken[parent]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Choque en la Av 5", _schoolAdmin, _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].DeviceTokens,
            Is.EquivalentTo(new[] { "ExponentPushToken[school]", "ExponentPushToken[parent]" }));
        Assert.That(_push.Sent[0].Title, Is.EqualTo(NotifyIncidentReportedService.PushTitle));
        Assert.That(_push.Sent[0].Body, Is.EqualTo("Choque en la Av 5"));
    }

    [Test]
    public async Task Sends_one_push_when_a_recipient_has_several_devices()
    {
        AddDevice(_parentA, "ExponentPushToken[phone]");
        AddDevice(_parentA, "ExponentPushToken[tablet]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Frenazo", _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].DeviceTokens,
            Is.EquivalentTo(new[] { "ExponentPushToken[phone]", "ExponentPushToken[tablet]" }));
    }

    [Test]
    public async Task Does_not_push_to_devices_of_profiles_that_are_not_recipients()
    {
        var bystander = Guid.NewGuid();
        AddDevice(_parentA, "ExponentPushToken[parent]");
        AddDevice(bystander, "ExponentPushToken[bystander]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Frenazo", _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].DeviceTokens, Is.EqualTo(new[] { "ExponentPushToken[parent]" }));
    }

    [Test]
    public async Task Deduplicates_repeated_recipients_and_tokens()
    {
        AddDevice(_parentA, "ExponentPushToken[parent]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Frenazo", _parentA, _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].DeviceTokens, Is.EqualTo(new[] { "ExponentPushToken[parent]" }));
    }

    [Test]
    public async Task Does_nothing_when_no_recipient_has_a_device_registered()
    {
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Frenazo", _schoolAdmin, _parentA));

        Assert.That(_push.Sent, Is.Empty);
    }

    [Test]
    public async Task Does_nothing_when_there_are_no_recipients()
    {
        AddDevice(_parentA, "ExponentPushToken[parent]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("Frenazo"));

        Assert.That(_push.Sent, Is.Empty);
    }

    [Test]
    public async Task Falls_back_to_a_generic_body_when_the_description_is_missing()
    {
        AddDevice(_parentA, "ExponentPushToken[parent]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident(null, _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].Body, Is.EqualTo(NotifyIncidentReportedService.EmptyDescriptionBody));
    }

    [Test]
    public async Task Falls_back_to_a_generic_body_when_the_description_is_blank()
    {
        AddDevice(_parentA, "ExponentPushToken[parent]");
        await _context.SaveChangesAsync();

        await _service.NotifyAsync(Incident("   ", _parentA));

        Assert.That(_push.Sent, Has.Count.EqualTo(1));
        Assert.That(_push.Sent[0].Body, Is.EqualTo(NotifyIncidentReportedService.EmptyDescriptionBody));
    }
}
