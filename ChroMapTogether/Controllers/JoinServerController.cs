using ChroMapTogether.Models.Responses;
using ChroMapTogether.Registries;
using Microsoft.AspNetCore.Mvc;

namespace ChroMapTogether.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JoinServerController : ControllerBase
    {
        private readonly SessionRegistry sessionRegistry;

        public JoinServerController(SessionRegistry sessionRegistry)
            => this.sessionRegistry = sessionRegistry;

        [HttpGet]
        public ActionResult Get(string code)
        {
            var server = sessionRegistry.GetSession(code);

            return server is null
                ? NotFound()
                : Ok(new JoinSessionResponse
                {
                    ip = server.Ip,
                    port = server.Port
                });
        }
    }
}
