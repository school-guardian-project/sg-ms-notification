using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Dto.Request;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetIncidentsUseCase
{
    Task<IncidentsResponse> ExecuteAsync(IncidentFilters filters, CancellationToken ct = default);
}