namespace ms_notification.Api.Application.Dto.RouteDirectory;

// Ruta asignada del estudiante. Las horas viajan como HH:mm:ss desde ms-route y
// se exponen como TimeOnly. Con los schedules (dia + direccion + hora de salida)
// la ventana de escaneo se valida contra el dia operativo; StartTime/EndTime del
// par grueso quedan como respaldo cuando la ruta no tiene schedules.
public sealed record StudentRouteInfo(
    Guid RouteId,
    string RouteName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<StudentRouteSchedule> Schedules);
