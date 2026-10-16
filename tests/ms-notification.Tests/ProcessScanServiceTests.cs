using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Security;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Event;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

/// <summary>
/// FR-4.2 (register boarding), FR-4.3 (register alighting) and FR-5.1 (detect the
/// boarding event). ProcessScanService is the entry point of every scan, so these tests
/// pin down what it persists, what it publishes and each reason it refuses a scan.
/// </summary>
[TestFixture]
public class ProcessScanServiceTests
{
    private readonly Guid _driverId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Guid _routeId = Guid.NewGuid();
    private readonly Guid _routeStopId = Guid.NewGuid();
    private readonly Guid _stopId = Guid.NewGuid();

    private NotificationContext _context = null!;
    private QrTokenSigner _signer = null!;
    private RecordingEventPublisher _publisher = null!;
    private FakeRouteDirectory _routes = null!;
    private FakeFamilyDirectory _family = null!;
    private FakeStudentDirectory _students = null!;
    private ProcessScanService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationContext(options);
        _publisher = new RecordingEventPublisher();
        _routes = new FakeRouteDirectory();
        _family = new FakeFamilyDirectory();
        _students = new FakeStudentDirectory();

        _signer = new QrTokenSigner(new FakeConfiguration(new Dictionary<string, string?>
        {
            ["Qr:Secret"] = "test-secret",
            ["Qr:TtlSeconds"] = "300"
        }));

        _service = new ProcessScanService(
            _context, _publisher, _routes, _family, _students, _signer,
            NullLogger<ProcessScanService>.Instance);

        // Default, happy-path world: the driver is on a trip, the student has a route and
        // an assigned stop with a known location, and the family lookup succeeds.
        _routes.CurrentTrip = new CurrentTrip(_tripId, Guid.NewGuid(), "IN_PROGRESS");
        _routes.StudentRoute = new StudentRouteInfo(_routeId, "Ruta Mañana");
        _routes.StudentStopOnRoute = new StudentStopOnRoute(
            _routeStopId, _stopId, "Libertad", "Calle 12 #34-56", 4.6, -74.1);
        _family.ParentProfileIds = new[] { Guid.NewGuid() };
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private string ValidToken() => _signer.Issue(_studentId, "Juan Pérez", _routeId, _routeStopId).Token;

    private Task<ScanResponseDto> Scan(BoardingType type) =>
        _service.ExecuteAsync(_driverId, new ScanRequestDto
        {
            QrToken = ValidToken(),
            BoardingType = type
        });

    [Test]
    public async Task BoardingPersistsTheBoardingAndTheAlertAndPublishesTheEvent()
    {
        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.BoardingId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(response.AlertId, Is.Not.EqualTo(Guid.Empty));

        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
        var boarding = _context.Boardings.Single();
        Assert.That(boarding.ProfileId, Is.EqualTo(_studentId));
        Assert.That(boarding.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(boarding.RouteStopId, Is.EqualTo(_routeStopId));
        Assert.That(boarding.BoardingType, Is.EqualTo(BoardingType.ON_BOARD));

        Assert.That(_context.Alerts.Count(), Is.EqualTo(1));
        var alert = _context.Alerts.Single();
        Assert.That(alert.ProfileId, Is.EqualTo(_studentId));
        Assert.That(alert.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(alert.AlertTypeId, Is.EqualTo((byte)1));
        Assert.That(alert.Description, Does.Contain("Libertad").And.Contain("Calle 12 #34-56"));

        Assert.That(_publisher.Published, Has.Count.EqualTo(1));
        Assert.That(_publisher.Published[0].Topic, Is.EqualTo("student.scanned"));
        var published = (StudentScannedEvent)_publisher.Published[0].Payload;
        Assert.That(published.StudentProfileId, Is.EqualTo(_studentId));
        Assert.That(published.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(published.BoardingType, Is.EqualTo(BoardingType.ON_BOARD));
        Assert.That(published.StopName, Is.EqualTo("Libertad"));
        Assert.That(published.StopAddress, Is.EqualTo("Calle 12 #34-56"));
        Assert.That(published.Latitude, Is.EqualTo(4.6));
        Assert.That(published.Longitude, Is.EqualTo(-74.1));
    }

    [Test]
    public async Task TheScanReportsHowManyGuardiansWereReached()
    {
        _family.ParentProfileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.ParentsNotified, Is.EqualTo(2));
    }

    [Test]
    public async Task AlightingUsesTheSecondAlertTypeAndTheAlightingWording()
    {
        await Scan(BoardingType.ON_BOARD);

        var response = await Scan(BoardingType.OFF_BOARD);

        var alert = _context.Alerts.Single(a => a.AlertTypeId == 2);
        Assert.That(alert.Id, Is.EqualTo(response.AlertId));
        Assert.That(alert.Description, Does.Contain("bajó del bus"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(2));
    }

    [Test]
    public void AlightingWithoutAnActiveBoardingIsRejected()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.OFF_BOARD));

        Assert.That(ex!.Message, Does.Contain("No active boarding"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
        Assert.That(_publisher.Published, Is.Empty);
    }

    [Test]
    public async Task ARepeatedScanInsideTheWindowReusesTheBoardingAndDoesNotPublishTwice()
    {
        var first = await Scan(BoardingType.ON_BOARD);
        var second = await Scan(BoardingType.ON_BOARD);

        Assert.That(second.BoardingId, Is.EqualTo(first.BoardingId));
        Assert.That(second.AlertId, Is.EqualTo(first.AlertId));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
        Assert.That(_context.Alerts.Count(), Is.EqualTo(1));
        Assert.That(_publisher.Published, Has.Count.EqualTo(1));
    }

    [Test]
    public void AnUnknownBoardingTypeIsRejected()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(_driverId, new ScanRequestDto
            {
                QrToken = ValidToken(),
                BoardingType = (BoardingType)99
            }));

        Assert.That(ex!.Message, Does.Contain("Invalid boarding type"));
    }

    [Test]
    public void AnInvalidQrCredentialIsRejected()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(_driverId, new ScanRequestDto
            {
                QrToken = "not-a-signed-token",
                BoardingType = BoardingType.ON_BOARD
            }));

        Assert.That(ex!.Message, Does.Contain("Invalid or expired QR"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
    }

    [Test]
    public void ADriverWithoutAnActiveTripIsRejected()
    {
        _routes.CurrentTrip = null;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("no active trip"));
    }

    [Test]
    public void AStudentWithoutAnAssignedRouteIsRejected()
    {
        _routes.StudentRoute = null;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("no active route"));
    }

    [Test]
    public void AStudentWhoIsNotAssignedToTheRouteIsRejected()
    {
        _routes.StudentStopOnRoute = null;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("not assigned to this route"));
    }

    [Test]
    public async Task AFailingGuardianLookupDoesNotBreakTheScan()
    {
        _family.ThrowOnRead = new InvalidOperationException("user-management is down");

        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.ParentsNotified, Is.EqualTo(0));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
        Assert.That(_publisher.Published, Has.Count.EqualTo(1));
    }
}
