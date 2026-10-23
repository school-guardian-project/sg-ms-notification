using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class GetAlertsService : IGetAlertsUseCase
{
    private readonly NotificationContext _context;
    private readonly IStudentDirectory _studentDirectory;
    private readonly IRouteDirectory _routeDirectory;

    public GetAlertsService(
        NotificationContext context,
        IStudentDirectory studentDirectory,
        IRouteDirectory routeDirectory)
    {
        _context = context;
        _studentDirectory = studentDirectory;
        _routeDirectory = routeDirectory;
    }

    public async Task<AlertsResponse> ExecuteAsync(AlertFilters filters, CancellationToken ct = default)
    {
        var query = _context.Alerts
            .AsNoTracking();

        // Filtrar por tipo de alerta
        if (filters.AlertType.HasValue)
        {
            query = query.Where(a => a.AlertTypeId == (byte)filters.AlertType.Value);
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
            var search = filters.Search.ToLower();
            query = query.Where(a => a.Description != null && a.Description.ToLower().Contains(search));
        }

        // Ordenación
        var (sortField, sortDirection) = ParseSort(filters.Sort);
        query = sortDirection == "desc"
            ? query.OrderByDescending(e => EF.Property<DateTime>(e, sortField))
            : query.OrderBy(e => EF.Property<DateTime>(e, sortField));

        var totalElements = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalElements / (double)filters.Size);

        var alerts = await query
            .Skip(filters.Page * filters.Size)
            .Take(filters.Size)
            .ToListAsync(ct);

        // Resolver nombres de estudiantes y rutas - usando mapper pattern
        var alertDtos = new List<AlertListDto>();
        foreach (var alert in alerts)
        {
            var student = await _studentDirectory.GetStudentByProfileIdAsync(alert.ProfileId, ct);
            var route = await _routeDirectory.GetRouteByExecutionIdAsync(alert.RouteExecutionId, ct);

            alertDtos.Add(AlertListDtoMapper.ToDto(alert, student, route));
        }

        return new AlertsResponse
        {
            Content = alertDtos.ToArray(),
            TotalElements = totalElements,
            TotalPages = totalPages,
            Size = filters.Size,
            Number = filters.Page
        };
    }

    private static (string field, string direction) ParseSort(string sort)
    {
        var parts = sort.Split(',');
        return (parts[0], parts.Length > 1 ? parts[1] : "desc");
    }
}