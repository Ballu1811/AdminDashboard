using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/platform/companies")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public sealed class PlatformAdministrationController : ControllerBase
    {
        private readonly ICompanyManagementService _companies;

        public PlatformAdministrationController(ICompanyManagementService companies)
        {
            _companies = companies;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _companies.GetAllAsync());
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            return Ok(await _companies.GetModulesAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var company = await _companies.GetByIdAsync(id);
            return company == null ? NotFound() : Ok(company);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveCompanyRequest request)
        {
            try
            {
                return Ok(await _companies.CreateAsync(request));
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveCompanyRequest request)
        {
            try
            {
                var company = await _companies.UpdateAsync(id, request);
                return company == null ? NotFound() : Ok(company);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
    }
}
