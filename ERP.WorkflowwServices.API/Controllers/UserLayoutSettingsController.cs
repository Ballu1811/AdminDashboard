using ERP.WorkflowwServices.API.Common;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLayoutSettingsController : ControllerBase
    {
        private readonly IUserLayoutSettingsService _service;
        private readonly IUserContext _context;
        public UserLayoutSettingsController(IUserLayoutSettingsService service, IUserContext context)
        {
            _service = service;
            _context = context;
        }

        /* =========================================================
           🔹 GET RAW USER SETTINGS
           👉 Only user saved values (no fallback)
        ========================================================= */
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = _context.UserId;
            var tenantId = _context.TenantId;

            var data = await _service.GetAsync(userId, tenantId);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        /* =========================================================
           🔥 GET EFFECTIVE SETTINGS (USER + TENANT MERGED)
           👉 THIS IS MAIN API (Frontend always use this)
        ========================================================= */
        [HttpGet("effective")]
        public async Task<IActionResult> GetEffective()
        {
            var userId = _context.UserId;
            var tenantId = _context.TenantId;

            var result = await _service.GetEffectiveAsync(userId, tenantId);
            return Ok(result);
        }

        /* =========================================================
           🔥 SAVE (UPSERT USER SETTINGS)
        ========================================================= */
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserLayoutSettingsDto dto)
            {
            if (dto == null)
                return BadRequest("Invalid data");

            var token = HttpContext.Request.Headers["Authorization"].ToString();

            // 🔥 FORCE from context (security)
            dto.UserId = _context.UserId;
            dto.TenantId = _context.TenantId;

            var result = await _service.SaveAsync(dto);
            return Ok(result);
        }

        /* =========================================================
           🔥 RESET USER SETTINGS → FALLBACK TO TENANT
        ========================================================= */
        [HttpDelete("reset")]
        public async Task<IActionResult> Reset()
        {
            var userId = _context.UserId;
            var tenantId = _context.TenantId;

            await _service.ResetToTenantAsync(userId, tenantId);
            return Ok(new { message = "Reset to tenant default successful" });
        }
    }
}
