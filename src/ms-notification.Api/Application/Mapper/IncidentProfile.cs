using AutoMapper;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Application.Mapper;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

public class IncidentProfile : Profile
{
    public IncidentProfile()
    {
        CreateMap<Alert, IncidentListDto>()
            .ForMember(dest => dest.IncidentType, opt => opt.MapFrom(src => IncidentTypeMapper.Map(src.AlertTypeId)))
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => IncidentSeverityMapper.Map(src.AlertTypeId)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => IncidentStatus.Open))
            .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ResolvedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RouteName, opt => opt.Ignore())
            .ForMember(dest => dest.DriverName, opt => opt.Ignore())
            .ForMember(dest => dest.BusPlate, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ReportedAt, opt => opt.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.ReportedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RouteExecutionId, opt => opt.MapFrom(src => src.RouteExecutionId));
    }
}