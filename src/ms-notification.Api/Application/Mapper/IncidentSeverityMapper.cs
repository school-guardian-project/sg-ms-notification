using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Mapper;

internal static class IncidentSeverityMapper
{
    public static IncidentSeverity Map(byte alertTypeId)
    {
        return alertTypeId switch
        {
            1 => IncidentSeverity.Medium,
            2 => IncidentSeverity.High,
            3 => IncidentSeverity.Medium,
            4 => IncidentSeverity.Critical,
            5 => IncidentSeverity.Low,
            6 => IncidentSeverity.Medium,
            7 => IncidentSeverity.High,
            8 => IncidentSeverity.High,
            9 => IncidentSeverity.Medium,
            10 => IncidentSeverity.Medium,
            11 => IncidentSeverity.Low,
            12 => IncidentSeverity.Low,
            13 => IncidentSeverity.Critical,
            14 => IncidentSeverity.High,
            15 => IncidentSeverity.Medium,
            16 => IncidentSeverity.Medium,
            17 => IncidentSeverity.Low,
            18 => IncidentSeverity.Medium,
            19 => IncidentSeverity.Critical,
            20 => IncidentSeverity.Critical,
            21 => IncidentSeverity.Critical,
            22 => IncidentSeverity.Critical,
            23 => IncidentSeverity.High,
            24 => IncidentSeverity.Critical,
            _ => IncidentSeverity.Medium
        };
    }
}