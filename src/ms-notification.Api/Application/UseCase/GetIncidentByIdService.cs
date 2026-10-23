using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

/// <summary>
/// GET de un incidente por id para el dashboard admin. Aplica el mismo filtro
/// de tipos de incidente que el listado y reutiliza el mismo mapping de
/// AutoMapper, para que el DTO del detalle coincida con el de la lista.
/// </summary>
public class GetIncidentByIdService : IGetIncidentByIdUseCase
{
    private readonly NotificationContext _context;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IMapper _mapper;

    public GetIncidentByIdService(
        NotificationContext context,
        IRouteDirectory routeDirectory,
        IMapper mapper)
    {
        _context = context;
        _routeDirectory = routeDirectory;
        _mapper = mapper;
    }

    public async Task<IncidentListDto?> ExecuteAsync(Guid incidentId, CancellationToken ct = default)
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
        return _mapper.Map<IncidentListDto>(alert, opts =>
        {
            opts.Items["RouteName"] = route?.RouteName ?? "Sin ruta";
            opts.Items["DriverName"] = "Por resolver"; // TODO: resolver conductor
            opts.Items["BusPlate"] = "Por resolver"; // TODO: resolver bus
            opts.Items["ReportedBy"] = "Por resolver"; // TODO: resolver quién reportó
        });
    }
}
