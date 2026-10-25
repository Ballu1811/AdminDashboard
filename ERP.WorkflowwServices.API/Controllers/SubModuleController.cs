using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
public sealed class SubModuleController : ControllerBase
{
    private readonly ISubModuleService _service;
    public SubModuleController(ISubModuleService service) => _service = service;

    [HttpPost("getAll")]
    public async Task<IActionResult> GetAll([FromBody] FilterModel filter) => Ok(await _service.GetAllAsync(filter));

    [HttpGet("by-parent/{parentId:guid}")]
    public async Task<IActionResult> GetByParent(Guid parentId) => Ok(await _service.GetByParentIdAsync(parentId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound("Submodule not found") : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubModuleCreateDto dto)
    {
        try { return Ok(await _service.CreateAsync(dto)); }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException) { return BadRequest(exception.Message); }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] SubModuleUpdateDto dto)
    {
        try { await _service.UpdateAsync(dto); return Ok("Submodule updated successfully"); }
        catch (KeyNotFoundException) { return NotFound("Submodule not found"); }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id)
    {
        try { await _service.ArchiveAsync(id); return Ok("Submodule archived successfully"); }
        catch (KeyNotFoundException) { return NotFound("Submodule not found"); }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        try { await _service.RestoreAsync(id); return Ok("Submodule restored successfully"); }
        catch (KeyNotFoundException) { return NotFound("Submodule not found"); }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }
}
