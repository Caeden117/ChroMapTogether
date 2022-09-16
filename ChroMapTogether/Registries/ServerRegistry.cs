using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Timers;

namespace ChroMapTogether.Registries
{
    public class ServerRegistry
    {
        private readonly List<ChroMapServer> servers = new();
        private readonly IOptions<ServerConfiguration> config;
        private readonly ILogger logger;
        private readonly Timer timer;

        public ServerRegistry(IOptions<ServerConfiguration> config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;

            timer = new(config.Value.RoomExpiryPeriod * 60 * 1000)
            {
                AutoReset = true
            };
            timer.Elapsed += CheckForExpiredServers;
            timer.Start();
        }

        public void AddServer(ChroMapServer server)
        {
            server.expiry = DateTime.Now.AddMinutes(config.Value.RoomExpiryPeriod);
            servers.RemoveAll(x => x.ip == server.ip && x.port == server.port);
            servers.Add(server);

            logger.Information("New server created.");
        }

        public ChroMapServer? GetServer(string code) => servers.Find(x => x.code == code);

        public ChroMapServer? GetServer(Guid guid) => servers.Find(x => x.guid == guid);

        private void CheckForExpiredServers(object sender, ElapsedEventArgs e)
        {
            var expiredServers = servers.FindAll(x => x.expiry < e.SignalTime);

            logger.Information("Removed {0} expired servers.", expiredServers.Count);

            expiredServers.ForEach(x => servers.Remove(x));
        }
    }
}
