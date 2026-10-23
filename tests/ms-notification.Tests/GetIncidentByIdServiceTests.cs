using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Dto.RouteDirectory;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Application.UseCase;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Tests.Fakes;
using NUnit.Framework;

namespace ms_notification.Tests;

/// <summary>
/// GET de un incidente por id para el dashboard admin. Debe aplicar el mismo
/// filtro de tipos de incidente que el listado (las alertas que no son
/// incidentes no se ven) y devolver el mismo DTO que la lista.
/// </summary>
[TestFixture]
public class GetIncidentByIdServiceTests
{
    private readonly Guid _tripId = Guid.NewGuid();

    private NotificationContext _context = null!;
    private FakeRouteDirectory _routes = null!;
    private GetIncidentByIdService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<NotificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new NotificationContext(options);
        _routes = new FakeRouteDirectory
        {
            RouteByExecutionId = new RouteInfo(_tripId, "Ruta Mañana")
        };

        // Mismo profile que en producción: IncidentProfile, el único perfil que
        // mapea Alert -> IncidentListDto.
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<IncidentProfile>(), NullLoggerFactory.Instance).CreateMapper();

        _service = new GetIncidentByIdService(_context, _routes, mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Guid AddAlert(byte alertTypeId)
    {
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            RouteExecutionId = _tripId,
            AlertTypeId = alertTypeId,
            ProfileId = Guid.NewGuid(),
            DateTime = new DateTime(2026, 3, 16, 15, 30, 0, DateTimeKind.Utc),
            Description = "Choque leve en la Av. 68"
        };

        _context.Alerts.Add(alert);
        _context.SaveChanges();
        return alert.Id;
    }

    [Test]
    public async Task Get_AnUnknownIncident_ReturnsNull()
    {
        var dto = await _service.ExecuteAsync(Guid.NewGuid());

        Assert.That(dto, Is.Null);
    }

    [Test]
    public async Task Get_AnAlertOutsideTheIncidentTypes_ReturnsNull()
    {
        // 99 no esta en el filtro de tipos de incidente: se comporta como
        // "no existe" para el dashboard, igual que en el listado.
        var alertId = AddAlert(alertTypeId: 99);

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto, Is.Null);
    }

    [Test]
    public async Task Get_AnExistingIncident_ReturnsTheSameShapeAsTheList()
    {
        // 21 = Accident (Critical) en los mappers de tipo/gravedad.
        var alertId = AddAlert(alertTypeId: 21);

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo(alertId));
        Assert.That(dto.RouteExecutionId, Is.EqualTo(_tripId));
        // IncidentProfile ignora RouteName/DriverName/BusPlate (los resuelve otra
        // capa despues); el detalle debe devolverlos igual que la lista.
        Assert.That(dto.RouteName, Is.Empty);
        Assert.That(dto.DriverName, Is.Empty);
        Assert.That(dto.BusPlate, Is.Empty);
        Assert.That(dto.IncidentType, Is.EqualTo(IncidentType.Accident));
        Assert.That(dto.Severity, Is.EqualTo(IncidentSeverity.Critical));
        Assert.That(dto.Status, Is.EqualTo(IncidentStatus.Open));
        Assert.That(dto.Description, Is.EqualTo("Choque leve en la Av. 68"));
        Assert.That(dto.ReportedAt, Is.EqualTo(new DateTime(2026, 3, 16, 15, 30, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task Get_AResolvedIncident_MatchesTheListWhichStillReportsItOpen()
    {
        // La lista no tiene campo Status todas Open (no hay Status en Alert);
        // el detalle debe comportarse igual, sin importar AcknowledgedAt.
        var alertId = AddAlert(alertTypeId: 21);
        var alert = await _context.Alerts.SingleAsync(a => a.Id == alertId);
        alert.AcknowledgedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var dto = await _service.ExecuteAsync(alertId);

        Assert.That(dto!.Status, Is.EqualTo(IncidentStatus.Open));
    }
}
