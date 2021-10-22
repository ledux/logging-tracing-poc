using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tracing.IntegrationApi.Models;

namespace Tracing.IntegrationApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IntegrationController : ControllerBase
    {
        private readonly ILogger<IntegrationController> _logger;

        public IntegrationController(ILogger<IntegrationController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Post(Data data)
        {
            return Ok(data);
        }
    }
}
