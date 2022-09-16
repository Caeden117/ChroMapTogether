using ChroMapTogether.Configuration;
using ChroMapTogether.Registries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace ChroMapTogether.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeepServerAliveController : ControllerBase
    {
        private readonly ServerRegistry serverRegistry;
        private readonly IOptions<ServerConfiguration> config;

        public KeepServerAliveController(ServerRegistry serverRegistry, IOptions<ServerConfiguration> config)
        {
            this.serverRegistry = serverRegistry;
            this.config = config;
        }

        [HttpPut]
        public ActionResult Put(string guid)
        {
            if (!Guid.TryParse(guid, out var serverGuid))
                return BadRequest();

            var server = serverRegistry.GetServer(serverGuid);

            if (server is null)
                return NotFound();

            server.expiry = DateTime.Now.AddMinutes(config.Value.RoomExpiryPeriod);

            return Ok();
        }
    }
}
