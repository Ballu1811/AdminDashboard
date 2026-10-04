using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserManagementService _users;

        public UserController(IUserManagementService users)
        {
            _users = users;
        }

        [HttpPost("GetAll")]
        public async Task<IActionResult> GetAll([FromBody] FilterModel filter)
        {
            return Ok(await _users.GetAllAsync(filter));
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            return Ok(await _users.GetRolesAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _users.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                var user = await _users.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _users.UpdateAsync(id, request);
                return user == null ? NotFound() : Ok(user);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPut("{id:guid}/roles")]
        public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetUserRolesRequest request)
        {
            try
            {
                var user = await _users.SetRolesAsync(id, request);
                return user == null ? NotFound() : Ok(user);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetUserStatusRequest request)
        {
            try
            {
                var user = await _users.SetStatusAsync(id, request.IsActive);
                return user == null ? NotFound() : Ok(user);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
    }
}