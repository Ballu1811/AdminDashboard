using AutoMapper;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IWFEvent _wFEvent;
        public WorkflowController(IMapper mapper, IWFEvent wfEvent)
        {
            _mapper = mapper;
            _wFEvent = wfEvent;
        }

        [HttpPost("addWFEvent")]
        public async Task<IActionResult> AddWFEvent([FromBody] WFEventDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = _mapper.Map<WFEvent>(model);

            var result = await _wFEvent.AddWFEvent(entity);
            return result switch
            {
                > 0 => Ok(new
                {
                    success = true,
                    message = "WF Event saved successfully",
                    id = result
                }),

                -1 => Conflict(new { message = "Duplicate record" }),
                -2 => NotFound(new { message = "Record not found" }),

                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                })
            };
        }

        [HttpGet("GetWFEventId/{id}")]
        public async Task<IActionResult> GetWFEventId(int id)
        {
            var entity = await _wFEvent.GetWFEventIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "Event not found" });
            var dto = _mapper.Map<WFEventResponseDto>(entity);
            return Ok(new
            {
                status = true,
                data = dto
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetWFEvents([FromQuery] int status = 0)
        {
            var data = await _wFEvent.GetWFEventAsync(status);

            var result = _mapper.Map<IEnumerable<WFEventResponseDto>>(data);

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpGet("by-company")]
        public async Task<IActionResult> GetEventByCompany([FromQuery] int status, [FromQuery] Guid tenantId)
        {
            var data = await _wFEvent.GetEventByCompanyAsync(status, tenantId);

            var result = _mapper.Map<IEnumerable<WFEventResponseDto>>(data);
            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterWFEvent([FromBody] WorkFlowFilterModel filter, [FromQuery] string? keyword)
        {
            var (data, total) = await _wFEvent.FilterWFEventAsync(filter, keyword);

            var result = _mapper.Map<IEnumerable<WFEventResponseDto>>(data);
            return Ok(new
            {
                success = true,
                totalRecords = total,
                data = result
            });
        }

        [HttpPost("toggle-delete")]
        public async Task<IActionResult> ToggleDelete([FromBody] List<int> eventIds)
        {
            if (eventIds == null || !eventIds.Any())
                return BadRequest("Invalid ids");

            var userId = 0;

            var affected = await _wFEvent.ToggleDeleteWFEventsAsync(eventIds, userId);
            return Ok(new
            {
                success = true,
                affectedRows = affected
            });
        }
    }
}
