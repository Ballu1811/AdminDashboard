using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
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

        // ==========================================
        // GET : api/menu/{id}
        // ==========================================
        [HttpGet("{id:guid}")]
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
        public async Task<IActionResult> Create([FromBody] MenuCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _menuService.ExistsAsync(dto.Label, dto.ParentId);

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
        public async Task<IActionResult> Update(Guid id, [FromBody] MenuUpdateDto model)
        {
            if (id != model.Id)
                return BadRequest(new
                {
                    message = "Id mismatch"
                });

            await _menuService.UpdateAsync(model);

            return Ok(new { message = "Menu updated successfully" });
        }

        // ==========================================
        // DELETE : api/menu/{id}
        // Soft Delete
        // ==========================================
        [HttpDelete("{id:guid}")]
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
    }
}
