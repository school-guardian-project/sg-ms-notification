namespace ms_notification.Api.Domain.Enum;

public enum IncidentType : byte
{
    Delay = 1,
    Breakdown = 2,
    Accident = 3,
    Deviation = 4,
    Other = 255
}