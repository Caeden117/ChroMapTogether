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
    public class SessionRegistry
    {
        private readonly List<Session> sessions = new();
        private readonly IOptions<ServerConfiguration> config;
        private readonly ILogger logger;
        private readonly Timer timer;

        public SessionRegistry(IOptions<ServerConfiguration> config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;

            timer = new(config.Value.RoomExpiryPeriod * 60 * 1000)
            {
                AutoReset = true
            };
            timer.Elapsed += CheckForExpiredSessions;
            timer.Start();
        }

        public void AddSession(Session session)
        {
            session.Expiry = DateTime.Now.AddMinutes(config.Value.RoomExpiryPeriod);
            sessions.RemoveAll(x => x.Ip == session.Ip && x.Port == session.Port);
            sessions.Add(session);

            logger.Information($"New session created.");
        }

        public void DeleteSession(Session session)
        {
            session.Close();
            sessions.Remove(session);

            logger.Information("Session explicitly removed.");
        }

        public Session? GetSession(string code) => sessions.Find(x => x.Code == code);

        public Session? GetSession(Guid guid) => sessions.Find(x => x.Guid == guid);

        public Session? GetSession(IPEndPoint hostAddress)
            => sessions.Find(x => x.Ip == hostAddress.Address.ToString() && x.Port == hostAddress.Port);

        public bool TryGetSession(string code, [MaybeNullWhen(false)] out Session session)
        {
            session = GetSession(code);
            return session != null;
        }

        private void CheckForExpiredSessions(object? sender, ElapsedEventArgs e)
        {
            var expiredSessions = sessions.FindAll(x => x.Expiry < e.SignalTime || x.ConnectedClients.Count == 0);

            if (expiredSessions.Count > 0)
            {
                logger.Information("Removed {0} expired sessions.", expiredSessions.Count);

                expiredSessions.ForEach(x => sessions.Remove(x));
            }
        }
    }
}
