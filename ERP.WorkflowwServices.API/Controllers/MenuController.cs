using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        // GET: api/menu/list
        [HttpPost]
        [Route("GetAllFlat")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAllFlat([FromBody] FilterModel filter)
        {
            var result = await _menuService.GetAllAsync(filter);
            return Ok(result);
        }

        // ==========================================
        // GET : api/menu
        // Sidebar Menu Tree
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetMenuTree()
        {
            var result = await _menuService.GetMenuTreeAsync();
            return Ok(result);
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetSidebarModules()
        {
            return Ok(await _menuService.GetSidebarModulesAsync());
        }

        // ==========================================
        // GET : api/menu/{id}
        // ==========================================
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await _menuService.GetByIdAsync(id);
            return data == null ? NotFound() : Ok(data);
        }

        // ==========================================
        // POST : api/menu
        // Create Menu
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] MenuCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _menuService.ExistsAsync(dto.Label, dto.ModuleId, dto.ParentId);

            if (exists)
                return Conflict(new
                {
                    message = "Menu with same name already exists"
                });

            var created = await _menuService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        // ==========================================
        // PUT : api/menu/{id}
        // Update Menu
        // ==========================================
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MenuUpdateDto model)
        {
            if (id != model.Id)
                return BadRequest(new
                {
                    message = "Id mismatch"
                });

            if (await _menuService.ExistsAsync(model.Label, model.ModuleId, model.ParentId, id))
                return Conflict(new { message = "A menu with this name already exists under the selected parent." });

            await _menuService.UpdateAsync(model);

            return Ok(new { message = "Menu updated successfully" });
        }

        // ==========================================
        // DELETE : api/menu/{id}
        // Soft Delete
        // ==========================================
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _menuService.GetByIdAsync(id);

            if (existing == null)
                return NotFound(new { message = "Menu not found" });

            await _menuService.DeleteAsync(id);

            return Ok(new { message = "Menu deleted successfully" });
        }

        [HttpGet("module/{moduleId}")]
        public async Task<IActionResult> GetByModule(Guid moduleId)
        {
            var result = await _menuService.GetMenuTreeByModuleAsync(moduleId);
            return Ok(result);
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetByModuleName([FromQuery] string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return BadRequest("Module name is required");

            var result = await _menuService.GetMenuTreeByModuleNameAsync(moduleName);
            return Ok(result);
        }

        [HttpGet("placements")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetPlacements()
        {
            return Ok(await _menuService.GetPlacementsAsync());
        }

        [HttpGet("placements/{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetPlacementById(Guid id)
        {
            var placement = await _menuService.GetPlacementByIdAsync(id);
            return placement == null ? NotFound() : Ok(placement);
        }

        [HttpPost("placements")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreatePlacement([FromBody] MenuPlacementCreateDto model)
        {
            try
            {
                var placement = await _menuService.CreatePlacementAsync(model);
                return CreatedAtAction(nameof(GetPlacementById), new { id = placement.Id }, placement);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("placements/{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdatePlacement(Guid id, [FromBody] MenuPlacementUpdateDto model)
        {
            if (id != model.Id)
                return BadRequest(new { message = "Id mismatch" });

            try
            {
                await _menuService.UpdatePlacementAsync(model);
                return Ok(new { message = "Menu link updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("placements/{id:guid}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeletePlacement(Guid id)
        {
            try
            {
                await _menuService.DeletePlacementAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
