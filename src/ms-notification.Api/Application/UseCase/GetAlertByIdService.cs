using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
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

    public GetAlertByIdService(
        NotificationContext context,
        IStudentDirectory studentDirectory,
        IRouteDirectory routeDirectory)
    {
        _context = context;
        _studentDirectory = studentDirectory;
        _routeDirectory = routeDirectory;
    }

    public async Task<AlertListDto?> ExecuteAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _context.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alertId, ct);

        if (alert is null)
        {
            return null;
        }

        var student = await _studentDirectory.GetStudentByProfileIdAsync(alert.ProfileId, ct);
        var route = await _routeDirectory.GetRouteByExecutionIdAsync(alert.RouteExecutionId, ct);

        return AlertListDtoMapper.ToDto(alert, student, route);
    }
}
