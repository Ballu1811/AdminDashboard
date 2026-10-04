using ERP.WorkflowwServices.API.DTOs;

namespace ERP.WorkflowwServices.API.Interfaces
{
    public interface ICompanyManagementService
    {
        Task<List<CompanyListDto>> GetAllAsync();
        Task<CompanyListDto?> GetByIdAsync(Guid id);
        Task<List<CompanyModuleOptionDto>> GetModulesAsync();
        Task<CompanyListDto> CreateAsync(SaveCompanyRequest request);
        Task<CompanyListDto?> UpdateAsync(Guid id, SaveCompanyRequest request);
    }
}
