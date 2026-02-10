using AutoMapper;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERP.WorkflowwServices.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IMapper _mapper; IConfiguration _configuration; private WorkflowDbContext _context;
        private readonly IWFEvent _wFEvent; private IWebHostEnvironment _hostingEnvironment;

    }
}
