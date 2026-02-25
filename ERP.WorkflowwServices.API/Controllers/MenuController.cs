using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            var menu = await _menuService.GetByIdAsync(id);

            if (menu == null)
                return NotFound(new { message = "Menu not found" });

            return Ok(menu);
        }

        // ==========================================
        // POST : api/menu
        // Create Menu
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MenuItem model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _menuService.ExistsAsync(
                model.Label,
                model.ParentId);

            if (exists)
                return Conflict(new
                {
                    message = "Menu with same name already exists"
                });

            var created = await _menuService.CreateAsync(model);

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
        public async Task<IActionResult> Update(Guid id,
            [FromBody] MenuItem model)
        {
            if (id != model.Id)
                return BadRequest(new
                {
                    message = "Id mismatch"
                });

            var existing = await _menuService.GetByIdAsync(id);

            if (existing == null)
                return NotFound(new { message = "Menu not found" });

            // update fields
            existing.Label = model.Label;
            existing.Icon = model.Icon;
            existing.Route = model.Route;
            existing.ParentId = model.ParentId;
            existing.OrderNo = model.OrderNo;
            existing.Permission = model.Permission;
            existing.MenuType = model.MenuType;
            existing.Tooltip = model.Tooltip;
            existing.IsExternal = model.IsExternal;
            existing.Target = model.Target;
            existing.Module = model.Module;
            existing.DefaultExpanded = model.DefaultExpanded;
            existing.IsActive = model.IsActive;
            existing.IsVisible = model.IsVisible;

            await _menuService.UpdateAsync(existing);

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
    }
}
