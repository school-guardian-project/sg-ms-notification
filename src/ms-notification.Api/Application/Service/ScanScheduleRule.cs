using Microsoft.Extensions.Logging;
using ms_notification.Api.Application.Configuration;
using ms_notification.Api.Application.Dto.RouteDirectory;

namespace ms_notification.Api.Application.Service;

/// <summary>
/// Regla pura de la ventana horaria del escaneo: el escaneo se acepta dentro de
/// la jornada operativa de la ruta con la holgura configurada, en la zona
/// horaria de la operacion (Colombia).
///
/// La ventana se arma con los SCHEDULES del dia local:
///   - Si la ruta tiene schedules y ninguno es del dia de hoy -> no opera,
///     el escaneo se rechaza.
///   - Si tiene schedules de hoy -> la ventana va de la primera salida al
///     ultimo regreso del dia, ampliados +/- MarginMinutes a cada lado.
///   - Si la ruta NO trae schedules (datos viejos sin migrar) se cae al par
///     grueso StartTime/EndTime, preservando el comportamiento anterior.
///
/// Todo se calcula sobre minutos del dia con aritmetica modular, asi cubrimos
/// la ruta que cruza la medianoche y la holgura que empuja la ventana fuera
/// de [0, 1440).
///
/// Es fail-closed: sin ventana conocida (sin schedules y sin par grueso
/// resoluble) se rechaza el escaneo.
/// </summary>
public static class ScanScheduleRule
{
    public static void EnsureWithinSchedule(
        StudentRouteInfo route, TimeZoneInfo timeZone, TimeProvider clock, ScanWindowOptions window)
    {
        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone);
        var nowMinutes = localNow.Hour * 60 + localNow.Minute;
        var margin = window.MarginMinutes;

        if (route.Schedules is not null && route.Schedules.Count > 0)
        {
            var todays = route.Schedules
                .Where(s => s.DayOfWeek == localNow.DayOfWeek)
                .Select(s => s.StartTime.Hour * 60 + s.StartTime.Minute)
                .ToList();

            // La ruta no programa servicio para el dia local: fuera de operacion.
            if (todays.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Route {route.RouteId} has no schedule for today ({localNow:ddd}); scan rejected");
            }

            var opensAt = todays.Min() - margin;
            var closesAt = todays.Max() + margin;

            var nowMod = Mod(nowMinutes);
            var from = Mod(opensAt);
            var to = Mod(closesAt);

            var within = from <= to
                ? nowMod >= from && nowMod <= to
                : nowMod >= from || nowMod <= to;

            if (!within)
            {
                throw new InvalidOperationException(
                    "Scan is outside the route schedule window");
            }

            return;
        }

        // ---------- Respaldo: ruta sin schedules (par grueso StartTime/EndTime) ----------
        var startMinutes = route.StartTime.Hour * 60 + route.StartTime.Minute;
        var endMinutes = route.EndTime.Hour * 60 + route.EndTime.Minute;

        // Sin schedules Y sin jornada gruesa resoluble (TimeOnly.MinValue): no hay
        // ventana conocida -> se rechaza el escaneo. Antes esto era fail-open y
        // dejaba pasar todo; ahora que todas las rutas deberian tener schedules
        // el caso legacy es un dato corrupto que no debe silenciarse.
        if (startMinutes == 0 && endMinutes == 0)
        {
            throw new InvalidOperationException(
                "Route has no schedule and no coarse window; scan rejected");
        }

        var opensAtLegacy = startMinutes - margin;
        var closesAtLegacy = endMinutes + margin;

        var nowModLegacy = Mod(nowMinutes);
        var fromLegacy = Mod(opensAtLegacy);
        var toLegacy = Mod(closesAtLegacy);

        var withinLegacy = fromLegacy <= toLegacy
            ? nowModLegacy >= fromLegacy && nowModLegacy <= toLegacy
            : nowModLegacy >= fromLegacy || nowModLegacy <= toLegacy;

        if (!withinLegacy)
        {
            throw new InvalidOperationException(
                "Scan is outside the route schedule window");
        }
    }

    private static int Mod(int minutes) => ((minutes % (24 * 60)) + 24 * 60) % (24 * 60);

    public static TimeZoneInfo ResolveTimeZone(string id, ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Colombia no tiene horario de verano: UTC-5 fijo como respaldo para
            // que una zona ausente no cambie las reglas de negocio.
            logger.LogWarning(ex, "Unknown time zone '{TimeZoneId}'; falling back to UTC-05:00", id);
            return TimeZoneInfo.CreateCustomTimeZone("scan-window", TimeSpan.FromHours(-5), "UTC-05:00", "UTC-05:00");
        }
    }
}
