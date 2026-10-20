using System.Text.Json.Serialization;

namespace ms_notification.Api.Domain.Enum;

/// <summary>
/// Valores de la columna Notification.Boarding.BoardingType
/// (CHECK IN ('ON_BOARD','OFF_BOARD')). El converter hace que el JSON de la
/// API use los mismos nombres que la base.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BoardingType
{
    ON_BOARD,
    OFF_BOARD
}
