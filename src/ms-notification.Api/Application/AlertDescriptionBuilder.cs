using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application;

/// <summary>
/// Arma el texto de la alerta que recibe el guardian cuando el estudiante sube
/// o baja del bus (FR-5.2 y FR-5.3). Es logica pura y sin dependencias a
/// proposito: el texto es lo que se muestra al guardian, asi que se prueba
/// directamente sin levantar base de datos ni directorios.
/// </summary>
public static class AlertDescriptionBuilder
{
    /// <summary>Limite de la columna Notification.Alert.Description.</summary>
    public const int MaxLength = 100;

    private const string DefaultStudentName = "Estudiante";

    public static string Build(
        BoardingType boardingType,
        string? studentName,
        string? routeName,
        string? stopName,
        string? stopAddress)
    {
        var action = boardingType == BoardingType.OFF_BOARD ? "bajó del bus" : "subió al bus";
        var who = string.IsNullOrWhiteSpace(studentName) ? DefaultStudentName : studentName;
        var text = $"{who} {action}";

        // La parada y su direccion son la ubicacion que pide el requisito
        // ("... with ... the location of the assigned stop").
        if (!string.IsNullOrWhiteSpace(stopName) || !string.IsNullOrWhiteSpace(stopAddress))
        {
            text += string.IsNullOrWhiteSpace(stopName)
                ? " en la parada"
                : $" en la parada {stopName}";
        }

        if (!string.IsNullOrWhiteSpace(stopAddress))
        {
            text += $" ({stopAddress})";
        }

        // La ruta va al final: si hay que recortar, se pierde antes el nombre de
        // la ruta que la ubicacion de la parada.
        if (!string.IsNullOrWhiteSpace(routeName))
        {
            text += $" - {routeName}";
        }

        return text.Length > MaxLength ? text[..MaxLength] : text;
    }
}
