using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

/// <summary>
/// GET de una alerta por id para el dashboard admin. Resuelve los mismos
/// datos que el listado (nombre del estudiante, ruta) con los mismos mappers,
/// para que el DTO del detalle no se despegue del DTO de la lista.
/// </summary>
public class GetAlertByIdService : IGetAlertByIdUseCase
{
    private readonly NotificationContext _context;
    private readonly IStudentDirectory _studentDirectory;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IAlertMapper _alertMapper;

    public GetAlertByIdService(
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

    public async Task<AlertDetailResponse?> ExecuteAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _context.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alertId, ct);

        if (alert is null)
        {
            return null;
        }

        return _alertMapper.ToDetailResponse(alert);
    }
}