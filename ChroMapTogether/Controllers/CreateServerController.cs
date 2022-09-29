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
        private readonly SessionRegistry sessionRegistry;
        private readonly SessionCodeProvider codeProvider;
        private readonly IOptions<ServerConfiguration> config;

        public CreateServerController(SessionRegistry sessionRegistry, SessionCodeProvider codeProvider,
            IOptions<ServerConfiguration> config)
        {
            this.sessionRegistry = sessionRegistry;
            this.codeProvider = codeProvider;
            this.config = config;
        }

        [HttpPost]
        public ActionResult Post()
        {
            var session = new Session
            {
                Guid = Guid.NewGuid(),
                Ip = Request.HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString(),
                Port = 6969,
                Code = codeProvider.Generate(config.Value.RoomCodeLength)
            };

            sessionRegistry.AddSession(session);

            return Ok(new CreateSessionResponse
            {
                guid = session.Guid,
                port = session.Port,
                code = session.Code
            });
        }
    }
}
