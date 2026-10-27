using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Application.Dto.Request;

namespace ms_notification.Api.Domain.Port.In;

public interface IGetAlertsUseCase
{
    Task<AlertsResponse> ExecuteAsync(AlertFilters filters, CancellationToken ct = default);
}