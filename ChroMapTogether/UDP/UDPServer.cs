using ChroMapTogether.Configuration;
using ChroMapTogether.Registries;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;

namespace ChroMapTogether.UDP
{
    public sealed class UDPServer : IDisposable, INetLogger, INatPunchListener
    {
        private readonly NetManager netManager;
        private readonly EventBasedNetListener eventBasedNetListener;
        private readonly ILogger logger;
        private readonly ServerRegistry serverRegistry;
        private readonly IOptions<ServerConfiguration> serverConfig;
        private readonly Timer timer;
        private readonly Dictionary<string, Host> hosts = new();

        public UDPServer(ILogger logger, ServerRegistry serverRegistry, IOptions<ServerConfiguration> serverConfig)
        {
            this.logger = logger.ForContext<UDPServer>();
            this.serverRegistry = serverRegistry;
            this.serverConfig = serverConfig;

            eventBasedNetListener = new();
            eventBasedNetListener.ConnectionRequestEvent += EventBasedNetListener_ConnectionRequestEvent;

            NetDebug.Logger = this;

            netManager = new NetManager(eventBasedNetListener);
            netManager.NatPunchEnabled = true;
            netManager.NatPunchModule.Init(this);
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

            if (request.Data.TryGetString(out var roomCode) && roomCode.Length == serverConfig.Value.RoomCodeLength)
            {
                var session = serverRegistry.GetServer(roomCode);
                
                if (session != null)
                {
                    request.Accept();
                    logger.Information("Successfully established UDP connection.");
                    return;
                }
            }

            logger.Warning("UDP connection not related to a server; killing connection request...");

            request.Reject();
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string roomCode)
        {
            var server = serverRegistry.GetServer(roomCode);

            if (server == null)
            {
                logger.Information("Attempted NAT hole punching for session that no longer exists.");

                if (hosts.ContainsKey(roomCode))
                {
                    hosts.Remove(roomCode);
                }
                return;
            }

            if (hosts.TryGetValue(roomCode, out var host))
            {
                netManager.NatPunchModule.NatIntroduce(
                    host.InternalAddr, host.ExternalAddr, localEndPoint, remoteEndPoint, roomCode);
            }
            else
            {
                server.ip = remoteEndPoint.Address.MapToIPv4().ToString();
                server.port = remoteEndPoint.Port;
                hosts[roomCode] = new(localEndPoint, remoteEndPoint, roomCode);
            }
        }

        // Do nothing: We are server
        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token) { }

        public void WriteNet(NetLogLevel level, string str, params object[] args) => logger.Information(str, args);

        public record Host(IPEndPoint InternalAddr, IPEndPoint ExternalAddr, string roomCode);
    }
}
