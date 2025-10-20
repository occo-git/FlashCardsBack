using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint
        /// GET: /health
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok("healthy");
        }
    }
}
