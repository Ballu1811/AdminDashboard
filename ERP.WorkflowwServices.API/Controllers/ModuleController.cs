using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class ModuleController : ControllerBase
    {
        private readonly IModuleService _service;
        private readonly IModuleDependencyService _dependencyService;
        public ModuleController(IModuleService service, IModuleDependencyService dependencyService)
        {
            _service = service;
            _dependencyService = dependencyService;
        }

        [HttpGet("dependencies")]
        public async Task<IActionResult> GetDependencies(CancellationToken cancellationToken)
            => Ok(await _dependencyService.GetAllAsync(cancellationToken));

        [HttpGet("{id:guid}/dependencies")]
        public async Task<IActionResult> GetDependencies(Guid id, CancellationToken cancellationToken)
        {
            var module = await _service.GetByIdAsync(id);
            if (module is null) return NotFound("Module not found");
            return Ok(await _dependencyService.GetByModuleAsync(id, cancellationToken));
        }

        [HttpPut("{id:guid}/dependencies")]
        public async Task<IActionResult> ReplaceDependencies(Guid id, [FromBody] SaveModuleDependenciesDto request, CancellationToken cancellationToken)
        {
            try
            {
                await _dependencyService.ReplaceAsync(id, request.RequiredModuleIds, cancellationToken);
                return Ok(await _dependencyService.GetByModuleAsync(id, cancellationToken));
            }
            catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        }

        // ===============================
        // GET ALL MODULES
        // ===============================
        [HttpPost]
        [Route("getAll")]
        public async Task<IActionResult> GetAll([FromBody] FilterModel filter)
        {
            var data = await _service.GetAllAsync(filter);
            return Ok(data);
        }

        // ===============================
        // GET VISIBLE MODULES (UI use)
        // ===============================
        //[HttpGet("visible")]
        //public async Task<IActionResult> GetVisible()
        //{
        //    var data = await _service.GetAllAsync();

        //    var filtered = data
        //        .Where(x => x.IsActive && x.IsVisible && x.IsLicensed && !x.IsDeleted)
        //        .OrderBy(x => x.OrderNo);

        //    return Ok(filtered);
        //}

        // ===============================
        // GET BY ID
        // ===============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound("Module not found");

            return Ok(data);
        }

        // ===============================
        // CREATE MODULE
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ModuleCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ===============================
        // UPDATE MODULE
        // ===============================
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ModuleUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                return Ok("Module updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ===============================
        // DELETE (SOFT DELETE)
        // ===============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok("Module deleted successfully");
            }
            catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
            catch (DbUpdateException) { return Conflict(new { message = "Module status could not be updated." }); }
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                await _service.RestoreAsync(id);
                return Ok("Module restored successfully");
            }
            catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
            catch (DbUpdateException) { return Conflict(new { message = "Module status could not be updated." }); }
        }

        [HttpPost("bulk-archive")]
        public async Task<IActionResult> BulkArchive([FromBody] List<Guid> ids)
        {
            try { return Ok(new { archived = await _service.BulkArchiveAsync(ids ?? []) }); }
            catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
            catch (DbUpdateException) { return Conflict(new { message = "Module status could not be updated." }); }
        }

        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<Guid> ids)
        {
            try { return Ok(new { restored = await _service.BulkRestoreAsync(ids ?? []) }); }
            catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
            catch (DbUpdateException) { return Conflict(new { message = "Module status could not be updated." }); }
        }
    }
}
