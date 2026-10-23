using ms_notification.Api.Domain.Enum;

namespace ms_notification.Api.Application.Mapper;

internal static class IncidentTypeMapper
{
    public static IncidentType Map(byte alertTypeId)
    {
        return alertTypeId switch
        {
            1 => IncidentType.Delay,
            2 => IncidentType.Deviation,
            3 => IncidentType.Other,
            4 => IncidentType.Other,
            5 => IncidentType.Other,
            6 => IncidentType.Other,
            7 => IncidentType.Other,
            8 => IncidentType.Other,
            9 => IncidentType.Other,
            10 => IncidentType.Other,
            11 => IncidentType.Delay,
            12 => IncidentType.Other,
            13 => IncidentType.Other,
            14 => IncidentType.Other,
            15 => IncidentType.Other,
            16 => IncidentType.Other,
            17 => IncidentType.Other,
            18 => IncidentType.Other,
            19 => IncidentType.Other,
            20 => IncidentType.Other,
            21 => IncidentType.Accident,
            22 => IncidentType.Other,
            23 => IncidentType.Breakdown,
            24 => IncidentType.Other,
            _ => IncidentType.Other
        };
    }
}