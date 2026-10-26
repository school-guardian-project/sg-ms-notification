using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

/// <summary>
/// GET de un incidente por id para el dashboard admin. Aplica el mismo filtro
/// de tipos de incidente que el listado y reutiliza el mismo mapping,
/// para que el DTO del detalle coincida con el de la lista.
/// </summary>
public class GetIncidentByIdService : IGetIncidentByIdUseCase
{
    private readonly NotificationContext _context;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IIncidentMapper _incidentMapper;

    public GetIncidentByIdService(
        NotificationContext context,
        IRouteDirectory routeDirectory,
        IIncidentMapper incidentMapper)
    {
        _context = context;
        _routeDirectory = routeDirectory;
        _incidentMapper = incidentMapper;
    }

    public async Task<IncidentListResponse?> ExecuteAsync(Guid incidentId, CancellationToken ct = default)
    {
        var alert = await _context.Alerts
            .AsNoTracking()
            .Where(a => a.Id == incidentId &&
                        IncidentTypeFilter.AlertTypeIds.Contains(a.AlertTypeId))
            .FirstOrDefaultAsync(ct);

        if (alert is null)
        {
            return null;
        }

        var route = await _routeDirectory.GetRouteByExecutionIdAsync(alert.RouteExecutionId, ct);
        return _incidentMapper.ToListResponse(
            alert,
            route?.RouteName ?? "Sin ruta",
            "Por resolver", // TODO: resolver conductor
            "Por resolver", // TODO: resolver bus
            "Por resolver", // TODO: resolver quién reportó
            IncidentStatus.Open,
            null,
            null);
    }
}