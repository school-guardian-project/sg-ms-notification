using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Domain.Port.Out;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class GetIncidentsService : IGetIncidentsUseCase
{
    private readonly NotificationContext _context;
    private readonly IStudentDirectory _studentDirectory;
    private readonly IRouteDirectory _routeDirectory;
    private readonly IMapper _mapper;

    public GetIncidentsService(
        NotificationContext context,
        IStudentDirectory studentDirectory,
        IRouteDirectory routeDirectory,
        IMapper mapper)
    {
        _context = context;
        _studentDirectory = studentDirectory;
        _routeDirectory = routeDirectory;
        _mapper = mapper;
    }

    public async Task<IncidentsResponse> ExecuteAsync(IncidentFilters filters, CancellationToken ct = default)
    {
        // Filtramos solo alertas que son incidentes (no boarding/alighting)
        var incidentAlertTypeIds = new byte[] { 1, 2, 3, 4, 11, 13, 21, 23 }; // Delay, Deviation, etc.

        var query = _context.Alerts
            .Where(a => incidentAlertTypeIds.Contains(a.AlertTypeId))
            .AsNoTracking();

        // Filtrar por tipo de incidente usando el mapper
        if (filters.IncidentType.HasValue)
        {
            query = query.Where(a => IncidentTypeMapper.Map(a.AlertTypeId) == filters.IncidentType.Value);
        }

        // Filtrar por gravedad usando el mapper
        if (filters.Severity.HasValue)
        {
            query = query.Where(a => IncidentSeverityMapper.Map(a.AlertTypeId) == filters.Severity.Value);
        }

        // Filtrar por estado (por ahora todas son Open ya que no hay campo Status en Alert)
        if (filters.Status.HasValue)
        {
            // TODO: cuando se agregue campo Status a Alert
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

        // Búsqueda por texto
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

        // Mapear con AutoMapper
        var incidentDtos = new List<IncidentListDto>();
        foreach (var alert in alerts)
        {
            var route = await _routeDirectory.GetRouteByExecutionIdAsync(alert.RouteExecutionId, ct);
            incidentDtos.Add(_mapper.Map<IncidentListDto>(alert, opts =>
            {
                opts.Items["RouteName"] = route?.RouteName ?? "Sin ruta";
                opts.Items["DriverName"] = "Por resolver"; // TODO: resolver conductor
                opts.Items["BusPlate"] = "Por resolver"; // TODO: resolver bus
                opts.Items["ReportedBy"] = "Por resolver"; // TODO: resolver quién reportó
            }));
        }

        return new IncidentsResponse
        {
            Content = incidentDtos.ToArray(),
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