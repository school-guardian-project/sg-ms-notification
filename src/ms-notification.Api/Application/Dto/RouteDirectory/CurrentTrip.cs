namespace ms_notification.Api.Application.Dto.RouteDirectory;

// RouteId permite comparar el viaje del conductor con la ruta del estudiante:
// sin el, un escaneo podia registrar en un viaje de otra ruta.
public sealed record CurrentTrip(Guid Id, Guid RouteId, Guid BusId, string Status);
