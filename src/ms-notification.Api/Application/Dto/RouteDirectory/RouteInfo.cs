namespace ms_notification.Api.Application.Dto.RouteDirectory;

// Información básica de la ruta para el dashboard.
public sealed record RouteInfo(
    Guid RouteId,
    string RouteName);
