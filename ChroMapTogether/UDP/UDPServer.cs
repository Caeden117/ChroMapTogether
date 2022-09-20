using ChroMapTogether.Registries;
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using System;
using System.Timers;

namespace ChroMapTogether.UDP
{
    public sealed class UDPServer : IDisposable, INetLogger
    {
        private readonly NetManager netManager;
        private readonly EventBasedNetListener eventBasedNetListener;
        private readonly ILogger logger;
        private readonly ServerRegistry serverRegistry;
        private readonly Timer timer;

        public UDPServer(ILogger logger, ServerRegistry serverRegistry)
        {
            this.logger = logger.ForContext<UDPServer>();
            this.serverRegistry = serverRegistry;

            eventBasedNetListener = new EventBasedNetListener();
            eventBasedNetListener.ConnectionRequestEvent += EventBasedNetListener_ConnectionRequestEvent;

            NetDebug.Logger = this;

            netManager = new NetManager(eventBasedNetListener);
            netManager.StartInManualMode(6969);

            timer = new Timer(1 / 60d * 1000);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            logger.Information("UDP server started.");
        }

        public void Dispose() => netManager.DisconnectAll();

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            netManager?.ManualReceive();
            netManager?.ManualUpdate((int)timer.Interval);
        }

        private void EventBasedNetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            logger.Information("Got an incoming UDP connection");

            if (request.Data.TryGetString(out var serverGuid) && Guid.TryParse(serverGuid, out var guid))
            {
                var session = serverRegistry.GetServer(guid);

                if (session != null)
                {
                    var peer = request.Accept();

                    logger.Information($"RREP Address: {request.RemoteEndPoint.Address}");
                    logger.Information($"RREP Port: {request.RemoteEndPoint.Port}");
                    logger.Information($"Peer Address: {peer.EndPoint.Address}");
                    logger.Information($"Peer Port: {peer.EndPoint.Port}");

                    session.ip = peer.EndPoint.Address.ToString();
                    session.port = peer.EndPoint.Port;

                    logger.Information("Successfully established UDP connection with a host.");
                    return;
                }
            }

            logger.Warning("UDP connection not related to a server; killing connection request...");

            request.Reject();
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            logger.Information(str, args);
        }
    }
}
