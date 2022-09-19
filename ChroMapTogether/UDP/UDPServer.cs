using ChroMapTogether.Registries;
using LiteNetLib;
using LiteNetLib.Utils;
using Serilog;
using System;
using System.Timers;

namespace ChroMapTogether.UDP
{
    public sealed class UDPServer : IDisposable
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
            eventBasedNetListener.PeerDisconnectedEvent += EventBasedNetListener_PeerDisconnectedEvent;

            netManager = new NetManager(eventBasedNetListener);
            netManager.Start(6969);

            timer = new Timer((1 / 60d) * 1000);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            logger.Information("UDP server started.");
        }

        public void Dispose() => netManager.DisconnectAll();

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) => netManager?.PollEvents();

        private void EventBasedNetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            logger.Information("Got an incoming UDP connection");

            if (request.Data.TryGetString(out var serverGuid) && Guid.TryParse(serverGuid, out var guid))
            {
                var server = serverRegistry.GetServer(guid);

                if (server != null)
                {
                    server.ip = request.RemoteEndPoint.Address.ToString();
                    server.port = request.RemoteEndPoint.Port;

                    logger.Information("Successfully established UDP connection with a host.");

                    var peer = request.Accept();

                    var netDataWriter = new NetDataWriter();
                    netDataWriter.Put(server.port);

                    peer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);

                    return;
                }
            }

            logger.Warning("UDP connection not related to a server; killing connection request...");

            request.Reject();
        }

        private void EventBasedNetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var server = serverRegistry.GetServer(peer.EndPoint);

            if (server != null)
            {
                logger.Information("UDP connection for host lost; deleting hosted session...");
                serverRegistry.DeleteServer(server);
            }
        }
    }
}
