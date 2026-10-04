using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public sealed class RoleController : ControllerBase
    {
        private readonly IRoleManagementService _roles;

        public RoleController(IRoleManagementService roles)
        {
            _roles = roles;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _roles.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveRoleRequest request)
        {
            try
            {
                return Ok(await _roles.CreateAsync(request));
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveRoleRequest request)
        {
            try
            {
                var role = await _roles.UpdateAsync(id, request);
                return role == null ? NotFound() : Ok(role);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
    }
}
