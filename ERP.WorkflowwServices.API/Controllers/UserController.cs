using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserManagementService _users;

        public UserController(IUserManagementService users)
        {
            _users = users;
        }

        [HttpPost("GetAll")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetAll([FromBody] FilterModel filter)
        {
            try
            {
                return Ok(await _users.GetAllAsync(filter));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("roles")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                return Ok(await _users.GetRolesAsync());
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("companies")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                return Ok(await _users.GetCompaniesAsync());
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("departments")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetDepartments([FromQuery] Guid? companyId)
        {
            try
            {
                return Ok(await _users.GetDepartmentsAsync(companyId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("positions")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetPositions([FromQuery] Guid? companyId)
        {
            try
            {
                return Ok(await _users.GetPositionsAsync(companyId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("branches")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetBranches([FromQuery] Guid? companyId)
        {
            try
            {
                return Ok(await _users.GetBranchesAsync(companyId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("user-types")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetUserTypes([FromQuery] Guid? companyId)
        {
            try
            {
                return Ok(await _users.GetUserTypesAsync(companyId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "UserManagement.View")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _users.GetByIdAsync(id);
                return user == null ? NotFound() : Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Authorize(Policy = "UserManagement.Create")]
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
            catch (EmailDeliveryException exception)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = exception.Message,
                    accountCreated = true,
                    invitationPending = true
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Employee Code is already in use or the selected profile mapping is no longer available." });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "UserManagement.Update")]
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Employee Code is already in use or the selected profile mapping is no longer available." });
            }
        }

        [HttpPut("{id:guid}/roles")]
        [Authorize(Policy = "UserManagement.Update")]
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "UserManagement.Status")]
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{id:guid}/resend-invitation")]
        [Authorize(Policy = "UserManagement.Invitation")]
        public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _users.ResendInvitationAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
            catch (EmailDeliveryException exception)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}