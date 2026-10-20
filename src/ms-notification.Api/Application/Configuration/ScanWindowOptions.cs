namespace ms_notification.Api.Application.Configuration;

/// <summary>
/// Ventana horaria del escaneo. Un QR solo se acepta dentro de la jornada de la
/// ruta (start_time..end_time), con una holgura hacia cada lado para no rechazar
/// al conductor que llega un par de minutos antes o despues.
///
/// Los valores por defecto del codigo coinciden con el .env de ms-notification:
/// asi el servicio sigue operando aunque la seccion no llegue por configuracion.
/// </summary>
public sealed class ScanWindowOptions
{
    public const string SectionName = "ScanWindow";

    /// <summary>Minutos de tolerancia antes de start_time y despues de end_time.</summary>
    public int MarginMinutes { get; set; } = 15;

    /// <summary>
    /// Zona horaria de la operacion (Colombia). Se resuelve por IANA en Linux,
    /// que es donde corren los contenedores.
    /// </summary>
    public string TimeZoneId { get; set; } = "America/Bogota";
}
