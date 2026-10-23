using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetIncidentsUseCase
{
    Task<IncidentsResponse> ExecuteAsync(IncidentFilters filters, CancellationToken ct = default);
}