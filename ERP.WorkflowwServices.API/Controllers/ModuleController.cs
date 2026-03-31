using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly IModuleService _service;
        public ModuleController(IModuleService service)
        {
            _service = service;
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
