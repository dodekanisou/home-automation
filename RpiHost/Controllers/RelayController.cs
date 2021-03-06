using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RpiHost.Models;
using RpiHost.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class RelayController : ControllerBase
    {
        private readonly RelayService relayService;
        private readonly ILogger<RelayController> logger;

        public RelayController(
            RelayService relayService,
            ILogger<RelayController> logger)
        {
            this.relayService = relayService;
            this.logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "relay:read")]
        public IEnumerable<AvailableRelay> GetDevices()
        {
            return this.relayService.GetDeviceList().Select(i => new AvailableRelay() { Icon = i.Icon, Id = i.Id, Name = i.Name });
        }

        [HttpPost]
        [Authorize(Policy = "relay:write")]
        public async Task<ActionResult> ActivateDevice([FromBody] RequestWithId<string> request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id))
            {
                logger.LogInformation("Recieved request with empty id");
                return BadRequest("Missing device id");
            }
            if (!relayService.DeviceExists(request.Id))
            {
                logger.LogInformation("Recieved request for unknown device id : {deviceId}", request.Id);
                return NotFound($"Device with id '{request.Id}' was not found");
            }

            await relayService.ActivateDevice(request.Id);

            return NoContent();
        }


    }
}
