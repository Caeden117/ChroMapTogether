using ChroMapTogether.Models.Responses;
using ChroMapTogether.Registries;
using Microsoft.AspNetCore.Mvc;

namespace ChroMapTogether.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JoinServerController : ControllerBase
    {
        private readonly ServerRegistry serverRegistry;

        public JoinServerController(ServerRegistry serverRegistry)
            => this.serverRegistry = serverRegistry;

        [HttpGet]
        public ActionResult Get(string code)
        {
            var server = serverRegistry.GetServer(code);

            return server is null
                ? NotFound()
                : Ok(new JoinServerResponse
                {
                    ip = server.ip,
                    port = server.port
                });
        }
    }
}
