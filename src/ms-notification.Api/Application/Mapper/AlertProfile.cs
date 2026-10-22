using AutoMapper;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Enum;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Application.Mapper;

public class AlertProfile : Profile
{
    public AlertProfile()
    {
        CreateMap<Alert, AlertListDto>()
            .ForMember(dest => dest.AlertType, opt => opt.MapFrom(src => (BoardingType)src.AlertTypeId))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.DateTime))
            .ForMember(dest => dest.Acknowledged, opt => opt.MapFrom(src => src.AcknowledgedAt.HasValue))
            .ForMember(dest => dest.AcknowledgedAt, opt => opt.MapFrom(src => src.AcknowledgedAt))
            .ForMember(dest => dest.AcknowledgedBy, opt => opt.Ignore())
            .ForMember(dest => dest.StudentName, opt => opt.Ignore())
            .ForMember(dest => dest.RouteName, opt => opt.Ignore());
    }
}