using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Models;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface IWFEvent 
    {
        Task<int> AddWFEvent(WFEvent item);
        Task<int> ToggleDeleteWFEventsAsync(List<int> eventIds, int userId);
        Task<(IEnumerable<WFEvent> Data, int Total)> FilterWFEventAsync(FilterModel paging, string? keyword);
        Task<IEnumerable<WFEvent>> GetWFEventAsync(int status);
        Task<IEnumerable<WFEvent>> GetEventByCompanyAsync(int status, Guid tenantId);
        Task<WFEvent?> GetWFEventIdAsync(int id);

        //IEnumerable<WFEvent> getEventByCompanyByPaginator(WorkFlowFilterModel filter, out int Total);

        //IEnumerable<WFEvent> VendorWFEventPaginate(WorkFlowFilterModel filter, out int Total);

    }
}
