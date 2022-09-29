using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
            server.Expiry = DateTime.Now.AddMinutes(config.Value.RoomExpiryPeriod);
            servers.RemoveAll(x => x.Ip == server.Ip && x.Port == server.Port);
            servers.Add(server);

            logger.Information($"New server created at {server.Ip}:{server.Port}.");
        }

        public void DeleteServer(ChroMapServer server)
        {
            server.Close();
            servers.Remove(server);

            logger.Information("Server explicitly removed.");
        }

        public ChroMapServer? GetServer(string code) => servers.Find(x => x.Code == code);

        public ChroMapServer? GetServer(Guid guid) => servers.Find(x => x.Guid == guid);

        public ChroMapServer? GetServer(IPEndPoint hostAddress)
            => servers.Find(x => x.Ip == hostAddress.Address.ToString() && x.Port == hostAddress.Port);

        public bool TryGetServer(string code, [MaybeNullWhen(false)] out ChroMapServer session)
        {
            session = GetServer(code);
            return session != null;
        }

        private void CheckForExpiredServers(object sender, ElapsedEventArgs e)
        {
            var expiredServers = servers.FindAll(x => x.Expiry < e.SignalTime || x.ConnectedClients.Count == 0);

            logger.Information("Removed {0} expired servers.", expiredServers.Count);

            expiredServers.ForEach(x => servers.Remove(x));
        }
    }
}
