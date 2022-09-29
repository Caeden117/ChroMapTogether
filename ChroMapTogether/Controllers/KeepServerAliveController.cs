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
        private readonly SessionRegistry sessionRegistry;
        private readonly IOptions<ServerConfiguration> config;

        public KeepServerAliveController(SessionRegistry sessionRegistry, IOptions<ServerConfiguration> config)
        {
            this.sessionRegistry = sessionRegistry;
            this.config = config;
        }

        [HttpPut]
        public ActionResult Put(string guid)
        {
            if (!Guid.TryParse(guid, out var sessionGuid))
                return BadRequest();

            var session = sessionRegistry.GetSession(sessionGuid);

            if (session is null)
                return NotFound();

            session.Expiry = DateTime.Now.AddMinutes(config.Value.RoomExpiryPeriod);

            return Ok();
        }
    }
}
