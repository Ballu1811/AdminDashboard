using AutoMapper;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Configurations
{
    public class MapperProfile:Profile
    {
        public MapperProfile()
        {
            CreateMap<WFEventDto, WFEvent>();
            CreateMap<WFEvent, WFEventResponseDto>();
        }
    }
}
