using ms_notification.Api.Application.Dto;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetAlertsUseCase
{
    Task<AlertsResponse> ExecuteAsync(AlertFilters filters, CancellationToken ct = default);
}