using ms_notification.Api.Application.Dto.Response;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

/// <summary>
/// Maps Scan (Boarding/Alert) domain entities to Response DTOs.
/// This mapper is stateless and can be registered as a singleton.
/// </summary>
public interface IScanResponseMapper
{
    ProcessScanResponse ToResponse(Boarding boarding, Alert alert, int parentsNotified, bool alreadyProcessed = false);
}

public sealed class ScanResponseMapper : IScanResponseMapper
{
    public ProcessScanResponse ToResponse(Boarding boarding, Alert alert, int parentsNotified, bool alreadyProcessed = false)
    {
        return new ProcessScanResponse(
            boarding.Id,
            alert.Id,
            parentsNotified,
            alreadyProcessed);
    }
}