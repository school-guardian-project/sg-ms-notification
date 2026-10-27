using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Dto.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ms_notification.Api.Application.Configuration;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Dto.RouteDirectory;
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

    // 10:00 en Colombia (UTC-5) es una hora dentro de la jornada por defecto
    // (06:00..08:00 + 15 min de holgura... ver SetUp). Se fija el reloj para que
    // la suite no dependa de la hora en que corre.
    private static readonly TimeOnly RouteStart = new(10, 0);
    private static readonly TimeOnly RouteEnd = new(12, 0);
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 16, 15, 30, 0, TimeSpan.Zero);

    private NotificationContext _context = null!;
    private QrTokenSigner _signer = null!;
    private RecordingEventPublisher _publisher = null!;
    private FakeRouteDirectory _routes = null!;
    private FakeFamilyDirectory _family = null!;
    private FakeStudentDirectory _students = null!;
    private FixedTimeProvider _clock = null!;
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
        _clock = new FixedTimeProvider(FixedNow);

        _signer = new QrTokenSigner(new FakeConfiguration(new Dictionary<string, string?>
        {
            ["Qr:Secret"] = "test-secret",
            ["Qr:TtlSeconds"] = "300"
        }));

        _service = new ProcessScanService(
            _context, _publisher, _routes, _family, _students, _signer,
            Options.Create(new ScanWindowOptions()),
            _clock,
            NullLogger<ProcessScanService>.Instance,
            new ScanResponseMapper());

        // Default, happy-path world: the driver is on a trip of the student's route,
        // the student has an assigned stop with a known location, and the family
        // lookup succeeds.
        _routes.CurrentTrip = new CurrentTrip(_tripId, _routeId, Guid.NewGuid(), "IN_PROGRESS");
        _routes.StudentRoute = new StudentRouteInfo(_routeId, "Ruta Mañana", RouteStart, RouteEnd,
            Array.Empty<StudentRouteSchedule>());
        _routes.StudentStopOnRoute = new StudentStopOnRoute(
            _routeStopId, _stopId, "Libertad", "Calle 12 #34-56", 4.6, -74.1);
        _family.ParentProfileIds = new[] { Guid.NewGuid() };
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private string ValidToken() => _signer.Issue(_studentId, "Juan Pérez", _routeId, _routeStopId).Token;

    private Task<ProcessScanResponse> Scan(BoardingType type) =>
        _service.ExecuteAsync(_driverId, new ProcessScanRequest
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
        // 25 = Boarding en el catalogo Notification.AlertType (seed 007).
        Assert.That(alert.AlertTypeId, Is.EqualTo((byte)25));
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

        // 26 = Alighting en el catalogo Notification.AlertType (seed 007).
        var alert = _context.Alerts.Single(a => a.AlertTypeId == 26);
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
    public async Task ARepeatedOnBoardScanReusesTheBoardingAndDoesNotNotifyAgain()
    {
        var first = await Scan(BoardingType.ON_BOARD);
        var second = await Scan(BoardingType.ON_BOARD);

        Assert.That(second.BoardingId, Is.EqualTo(first.BoardingId));
        Assert.That(second.AlertId, Is.EqualTo(first.AlertId));
        Assert.That(second.AlreadyProcessed, Is.True);
        Assert.That(first.AlreadyProcessed, Is.False);
        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
        Assert.That(_context.Alerts.Count(), Is.EqualTo(1));
        Assert.That(_publisher.Published, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ARepeatedOffBoardScanReusesTheAlightingAndDoesNotNotifyAgain()
    {
        await Scan(BoardingType.ON_BOARD);
        var first = await Scan(BoardingType.OFF_BOARD);

        // El reloj avanza fuera del estado: ya no hay ventana fija que reutilizar,
        // el estado (el estudiante ya descendio) es lo que hace idempotente el escaneo.
        _clock.Advance(TimeSpan.FromMinutes(5));
        var second = await Scan(BoardingType.OFF_BOARD);

        Assert.That(second.BoardingId, Is.EqualTo(first.BoardingId));
        Assert.That(second.AlertId, Is.EqualTo(first.AlertId));
        Assert.That(second.AlreadyProcessed, Is.True);
        Assert.That(_context.Boardings.Count(), Is.EqualTo(2));
        Assert.That(_context.Alerts.Count(), Is.EqualTo(2));
        Assert.That(_publisher.Published, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ABoardingAfterAlightingIsRegisteredAsANewBoarding()
    {
        await Scan(BoardingType.ON_BOARD);
        await Scan(BoardingType.OFF_BOARD);

        _clock.Advance(TimeSpan.FromMinutes(5));
        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.AlreadyProcessed, Is.False);
        Assert.That(_context.Boardings.Count(), Is.EqualTo(3));
        Assert.That(_publisher.Published, Has.Count.EqualTo(3));
    }

    [Test]
    public void AStudentFromAnotherRouteIsRejected()
    {
        _routes.StudentRoute = new StudentRouteInfo(Guid.NewGuid(), "Ruta Tarde", RouteStart, RouteEnd,
            Array.Empty<StudentRouteSchedule>());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("does not belong to this route"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
        Assert.That(_publisher.Published, Is.Empty);
    }

    [Test]
    public async Task AScanWithinTheScheduleMarginIsAccepted()
    {
        // 09:50 en Colombia = 10 min antes del inicio: dentro de los 15 de holgura.
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 14, 50, 0, TimeSpan.Zero));

        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.AlreadyProcessed, Is.False);
        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
    }

    [Test]
    public void AScanOutsideTheScheduleWindowIsRejected()
    {
        // 15:00 en Colombia: tres horas despues del cierre (+15 min de holgura).
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 20, 0, 0, TimeSpan.Zero));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("outside the route schedule window"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
        Assert.That(_publisher.Published, Is.Empty);
    }


    [Test]
    public async Task AScanWithinTodayScheduleIsAccepted()
    {
        // 10:30 Colombia (lunes): entre la salida 06:00 y el regreso 18:00,
        // con 15 min de holgura cada lado ([05:45, 18:15]).
        _routes.StudentRoute = new StudentRouteInfo(
            _routeId, "Ruta Mañana", RouteStart, RouteEnd,
            new[]
            {
                new StudentRouteSchedule(DayOfWeek.Monday, "Ida", new TimeOnly(6, 0)),
                new StudentRouteSchedule(DayOfWeek.Monday, "Vuelta", new TimeOnly(18, 0))
            });

        var response = await Scan(BoardingType.ON_BOARD);

        Assert.That(response.AlreadyProcessed, Is.False);
        Assert.That(_context.Boardings.Count(), Is.EqualTo(1));
    }

    [Test]
    public void AScanOnANonOperationalDayIsRejected()
    {
        // Martes 10:30 CO: la ruta solo tiene schedule el lunes.
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 17, 15, 30, 0, TimeSpan.Zero));

        _routes.StudentRoute = new StudentRouteInfo(
            _routeId, "Ruta Mañana", RouteStart, RouteEnd,
            new[]
            {
                new StudentRouteSchedule(DayOfWeek.Monday, "Ida", new TimeOnly(6, 0)),
                new StudentRouteSchedule(DayOfWeek.Monday, "Vuelta", new TimeOnly(18, 0))
            });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("no schedule for today"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
    }

    [Test]
    public void AScanOutsideTodayScheduleIsRejected()
    {
        // Lunes 04:00 CO: antes de la primera salida (06:00 - 15 min = 05:45).
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 09, 0, 0, TimeSpan.Zero));

        _routes.StudentRoute = new StudentRouteInfo(
            _routeId, "Ruta Mañana", RouteStart, RouteEnd,
            new[]
            {
                new StudentRouteSchedule(DayOfWeek.Monday, "Ida", new TimeOnly(6, 0)),
                new StudentRouteSchedule(DayOfWeek.Monday, "Vuelta", new TimeOnly(18, 0))
            });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => Scan(BoardingType.ON_BOARD));

        Assert.That(ex!.Message, Does.Contain("outside the route schedule window"));
        Assert.That(_context.Boardings.Count(), Is.EqualTo(0));
    }


    [Test]
    public void AnUnknownBoardingTypeIsRejected()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync(_driverId, new ProcessScanRequest
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
            _service.ExecuteAsync(_driverId, new ProcessScanRequest
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
