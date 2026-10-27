using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto.Request;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;
using ms_notification.Api.Infrastructure.Persistence.Entity;

namespace ms_notification.Api.Application.UseCase;

public class GetAlertsService : IGetAlertsUseCase
{
    private readonly NotificationContext _context;
    private readonly IStudentDirectory _studentDirectory;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IAlertMapper _alertMapper;

    public GetAlertsService(
        NotificationContext context,
        IStudentDirectory studentDirectory,
        IRouteDirectory routeDirectory,
        IAlertMapper alertMapper)
    {
        _context = context;
        _studentDirectory = studentDirectory;
        _routeDirectory = routeDirectory;
        _alertMapper = alertMapper;
    }

    public async Task<AlertsResponse> ExecuteAsync(AlertFilters filters, CancellationToken ct = default)
    {
        var query = _context.Alerts
            .AsNoTracking();

        if (filters.AdminProfileId.HasValue || filters.SchoolId.HasValue)
        {
            var schools = _context.Set<SchoolAdminRefEntity>()
                .Where(a => a.ProfileId == filters.AdminProfileId && a.Status == "Active")
                .Select(a => a.SchoolId);
            var profiles =
                from profile in _context.Set<ProfileRefEntity>()
                join campus in _context.Set<SchoolCampusRefEntity>()
                    on profile.CampuseId equals campus.Id
                where (!filters.AdminProfileId.HasValue || schools.Contains(campus.SchoolId))
                    && (!filters.SchoolId.HasValue || campus.SchoolId == filters.SchoolId)
                select profile.Id;
            query = query.Where(a => profiles.Contains(a.ProfileId));
        }

        // Filtrar por tipo de alerta
        if (filters.AlertTypeId.HasValue)
        {
            // alertTypeId se pasa directamente como byte (25=Boarding, 26=Alighting)
            query = query.Where(a => a.AlertTypeId == filters.AlertTypeId.Value);
        }
        else if (filters.AlertType.HasValue)
        {
            // Fallback: alertType=ON_BOARD/OFF_BOARD son los ids de escaneo 25/26
            // (007-insert-boarding-alert-types.sql); 1 y 2 del catalogo son
            // 'Bus Delay' y 'Route Deviation'.
            var scanAlertTypeId = filters.AlertType.Value == BoardingType.ON_BOARD
                ? (byte)25
                : (byte)26;
            query = query.Where(a => a.AlertTypeId == scanAlertTypeId);
        }

        // Filtrar por reconocida
        if (filters.Acknowledged.HasValue)
        {
            query = query.Where(a => a.AcknowledgedAt.HasValue == filters.Acknowledged.Value);
        }

        // Filtrar por rango de fechas
        if (filters.DateFrom.HasValue)
        {
            query = query.Where(a => a.DateTime >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(a => a.DateTime <= filters.DateTo.Value);
        }

        // Búsqueda por texto (en descripción)
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim().ToLower();
            query = query.Where(a => a.Description != null && a.Description.ToLower().Contains(search));
        }

        // Ordenación
        var (sortField, sortDirection) = ParseSort(filters.Sort);
        var orderedQuery = sortDirection == "desc"
            ? query.OrderByDescending(e => EF.Property<DateTime>(e, sortField))
            : query.OrderBy(e => EF.Property<DateTime>(e, sortField));
        query = orderedQuery.ThenBy(a => a.Id);

        var totalElements = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalElements / (double)filters.Size);

        var alerts = await query
            .Skip(filters.Page * filters.Size)
            .Take(filters.Size)
            .ToListAsync(ct);
        var typeIds = alerts.Select(a => a.AlertTypeId).Distinct().ToArray();
        var types = await _context.AlertTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        // Resolver nombres de estudiantes y rutas - usando mapper pattern
        var alertDtos = new List<AlertListResponse>();
        foreach (var alert in alerts)
        {
            var student = await _studentDirectory.GetStudentByProfileIdAsync(alert.ProfileId, ct);
            var route = await _routeDirectory.GetRouteByExecutionIdAsync(alert.RouteExecutionId, ct);

            alertDtos.Add(_alertMapper.ToListResponse(alert, student, route));
        }

        return new AlertsResponse(
            alertDtos.ToArray(),
            totalElements,
            totalPages,
            filters.Size,
            filters.Page);
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(',');
        return (nameof(Alert.DateTime), parts.Length > 1 && parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? "asc" : "desc");
    }
}