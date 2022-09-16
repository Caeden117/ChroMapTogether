using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
using ChroMapTogether.Models.Requests;
using ChroMapTogether.Models.Responses;
using ChroMapTogether.Providers;
using ChroMapTogether.Registries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace ChroMapTogether.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreateServerController : ControllerBase
    {
        private readonly ServerRegistry serverRegistry;
        private readonly ServerCodeProvider codeProvider;
        private readonly IOptions<ServerConfiguration> config;

        public CreateServerController(ServerRegistry serverRegistry, ServerCodeProvider codeProvider,
            IOptions<ServerConfiguration> config)
        {
            this.serverRegistry = serverRegistry;
            this.codeProvider = codeProvider;
            this.config = config;
        }

        [HttpPost]
        public ActionResult Post()
        {
            var server = new ChroMapServer
            {
                guid = Guid.NewGuid(),
                ip = HttpContext.Connection.RemoteIpAddress!.ToString(),
                port = HttpContext.Connection.RemotePort,
                code = codeProvider.Generate(config.Value.RoomCodeLength)
            };

            serverRegistry.AddServer(server);

            return Ok(new CreateServerResponse
            {
                guid = server.guid,
                port = server.port,
                code = server.code
            });
        }
    }
}
