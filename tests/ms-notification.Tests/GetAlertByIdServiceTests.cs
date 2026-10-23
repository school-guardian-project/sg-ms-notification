using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

/// <summary>
/// GET de una alerta por id para el dashboard admin. Debe devolver el mismo
/// DTO que la lista (estudiante y ruta resueltos) y null cuando no existe.
/// </summary>
[TestFixture]
public class GetAlertByIdServiceTests
{
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _tripId = Guid.NewGuid();

    private NotificationContext _context = null!;
    private FakeStudentDirectory _students = null!;
    private FakeRouteDirectory _routes = null!;
    private GetAlertByIdService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationContext(options);
        _students = new FakeStudentDirectory { StudentName = "Juan Pérez" };
        _routes = new FakeRouteDirectory
        {
            RouteByExecutionId = new RouteInfo(_tripId, "Ruta Mañana")
        };

        _service = new GetAlertByIdService(_context, _students, _routes);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Guid AddAlert(byte alertTypeId = 1, DateTime? acknowledgedAt = null)
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = _tripId,
            AlertTypeId = alertTypeId,
            ProfileId = _studentId,
            DateTime = new DateTime(2026, 3, 16, 15, 30, 0, DateTimeKind.Utc),
            Description = "Juan Pérez abordó el bus en la parada Libertad",
            AcknowledgedAt = acknowledgedAt
        };

        _context.Alerts.Add(alert);
        _context.SaveChanges();
        return alert.Id;
    }

    [Test]
    public async Task Get_AnUnknownAlert_ReturnsNull()
    {
        var dto = await _service.ExecuteAsync(Guid.NewGuid());

        Assert.That(dto, Is.Null);
    }

    [Test]
    public async Task Get_AnExistingAlert_ResolvesTheStudentAndTheRouteLikeTheList()
    {
        var alertId = AddAlert();

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo(alertId));
        Assert.That(dto.ProfileId, Is.EqualTo(_studentId));
        Assert.That(dto.RouteExecutionId, Is.EqualTo(_tripId));
        Assert.That(dto.StudentName, Is.EqualTo("Juan Pérez"));
        Assert.That(dto.RouteName, Is.EqualTo("Ruta Mañana"));
        Assert.That(dto.Message, Does.Contain("Libertad"));
        Assert.That(dto.Acknowledged, Is.False);
    }

    [Test]
    public async Task Get_AnAcknowledgedAlert_ReportsItAsAcknowledged()
    {
        var acknowledgedAt = new DateTime(2026, 3, 16, 16, 0, 0, DateTimeKind.Utc);
        var alertId = AddAlert(acknowledgedAt: acknowledgedAt);

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto!.Acknowledged, Is.True);
        Assert.That(dto.AcknowledgedAt, Is.EqualTo(acknowledgedAt));
    }

    [Test]
    public async Task Get_WhenTheRouteCannotBeResolved_FallsBackToTheListDefault()
    {
        _routes.RouteByExecutionId = null;
        var alertId = AddAlert();

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto!.RouteName, Is.EqualTo("Sin ruta"));
    }
}
