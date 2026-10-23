namespace ms_notification.Api.Application.Dto.RouteDirectory;

// Horario de la ruta: un dia de la semana, la direccion (Ida/Vuelta) y la hora
// de salida. ms-route lo envia como DayOfWeek.ToString() ("Monday", ...), que es
// el enum .NET; se parsea de vuelta al comparar contra el dia local de Colombia.
public sealed record StudentRouteSchedule(
    DayOfWeek DayOfWeek,
    string Direction,
    TimeOnly StartTime);
