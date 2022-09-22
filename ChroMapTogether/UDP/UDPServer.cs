using ChroMapTogether.Configuration;
using ChroMapTogether.Registries;
using ChroMapTogether.UDP.Packets;
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
    public sealed class UDPServer : IDisposable, INetLogger
    {
        private readonly NetManager netManager;
        private readonly EventBasedNetListener eventBasedNetListener;
        private readonly ILogger logger;
        private readonly ServerRegistry serverRegistry;
        private readonly IOptions<ServerConfiguration> serverConfig;
        private readonly Timer timer;

        private readonly Dictionary<string, List<NetPeer>> roomCodeToSession = new();
        private readonly Dictionary<NetPeer, string> peerToRoomCode = new();
        private readonly Dictionary<NetPeer, MapperIdentityPacket> cachedIdentities = new();
        private readonly Dictionary<NetPeer, MapperPosePacket> cachedPoses = new();
        private readonly List<NetPeer> peersNeedingMaps = new();

        public UDPServer(ILogger logger, ServerRegistry serverRegistry, IOptions<ServerConfiguration> serverConfig)
        {
            this.logger = logger.ForContext<UDPServer>();
            this.serverRegistry = serverRegistry;
            this.serverConfig = serverConfig;

            eventBasedNetListener = new();
            eventBasedNetListener.ConnectionRequestEvent += EventBasedNetListener_ConnectionRequestEvent;
            eventBasedNetListener.PeerDisconnectedEvent += EventBasedNetListener_PeerDisconnectedEvent;
            eventBasedNetListener.NetworkReceiveEvent += EventBasedNetListener_NetworkReceiveEvent;
            eventBasedNetListener.NetworkLatencyUpdateEvent += EventBasedNetListener_NetworkLatencyUpdateEvent;

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

            if (request.Data.TryGetString(out var roomCode) && roomCode.Length == serverConfig.Value.RoomCodeLength)
            {
                var session = serverRegistry.GetServer(roomCode);
                
                if (session != null)
                {
                    var peer = request.Accept();

                    if (!roomCodeToSession.TryGetValue(roomCode, out var peers))
                    {
                        peers = new();
                        roomCodeToSession.Add(roomCode, peers);
                    }
                    else
                    {
                        peersNeedingMaps.Add(peer);
                    }

                    var identity = request.Data.Get<MapperIdentityPacket>();
                    identity.ConnectionId = peers.Count;

                    // Send peer identities and poses to new user
                    foreach (var otherPeer in peers)
                    {
                        SendPacketFrom(identity, otherPeer, PacketId.MapperIdentity, identity);

                        var otherPeerIdentity = cachedIdentities[otherPeer];
                        SendPacketFrom(otherPeerIdentity, peer, PacketId.MapperIdentity, otherPeerIdentity);

                        if (cachedPoses.TryGetValue(otherPeer, out var lastKnownPose))
                        {
                            SendPacketFrom(otherPeerIdentity, peer, PacketId.MapperPose, lastKnownPose);
                        }
                    }

                    peers.Add(peer);
                    peerToRoomCode.Add(peer, roomCode);
                    cachedIdentities.Add(peer, identity);

                    logger.Information("Successfully established UDP connection.");
                    return;
                }
            }

            logger.Warning("UDP connection not related to a server; killing connection request...");

            request.Reject();
        }

        private void EventBasedNetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (peerToRoomCode.TryGetValue(peer, out var roomCode)
                && roomCodeToSession.TryGetValue(roomCode, out var otherPeers))
            {
                // Bogus identity from the client
                var _ = reader.GetByte();

                // We need to correct identity with the connection ID from the peer
                var identity = cachedIdentities[peer];

                var packetBytes = new byte[reader.AvailableBytes];
                Array.Copy(reader.RawData, reader.Position, packetBytes, 0, reader.AvailableBytes);

                var packetId = reader.GetByte();

                if (packetId == (byte)PacketId.SendZip)
                {
                    // Only forward zip packet to peers who need it
                    foreach (var otherPeer in otherPeers)
                    {
                        if (otherPeer != peer && peersNeedingMaps.Remove(otherPeer))
                        {
                            SendPacketFrom(identity, otherPeer, packetBytes);
                        }
                    }
                }
                else
                {
                    // Forward packet to other clients
                    foreach (var otherPeer in otherPeers)
                    {
                        if (otherPeer != peer)
                        {
                            SendPacketFrom(identity, otherPeer, packetBytes);
                        }
                    }
                }

                // Cache certain packets in case new client connects
                switch (packetId)
                {
                    case (byte)PacketId.MapperIdentity:
                        cachedIdentities[peer] = reader.Get<MapperIdentityPacket>();
                        break;

                    case (byte)PacketId.MapperPose:
                        cachedPoses[peer] = reader.Get<MapperPosePacket>();
                        break;
                }
            }
        }

        private void EventBasedNetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            peerToRoomCode.Remove(peer);
            cachedIdentities.Remove(peer);
            cachedPoses.Remove(peer);
            peersNeedingMaps.Remove(peer);

            if (peerToRoomCode.TryGetValue(peer, out var roomCode))
            {
                if (roomCodeToSession.TryGetValue(roomCode, out var otherPeers))
                {
                    // If the host is disconnected, kick everyone out
                    if (otherPeers.IndexOf(peer) == 0)
                    {
                        roomCodeToSession.Remove(roomCode);
                    
                        foreach (var otherPeer in otherPeers)
                        {
                            peer.Disconnect();
                        }

                        roomCodeToSession.Remove(roomCode);
                    }
                    else
                    {
                        otherPeers.Remove(peer);
                    }
                }

                peerToRoomCode.Remove(peer);
            }
        }

        private void EventBasedNetListener_NetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            if (peerToRoomCode.TryGetValue(peer, out var roomCode)
                && roomCodeToSession.TryGetValue(roomCode, out var otherPeers)
                && cachedIdentities.TryGetValue(peer, out var identity))
            {
                foreach (var otherPeer in otherPeers)
                {
                    if (otherPeer != peer)
                    {
                        SendPacketFrom(identity, otherPeer, PacketId.MapperLatency, new MapperLatencyPacket(latency));
                    }
                }
            }
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args) => logger.Information(str, args);
        
        public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, PacketId packetId, INetSerializable data)
        {
            var writer = new NetDataWriter();

            writer.Put(fromPeer.ConnectionId);
            writer.Put((byte)packetId);
            writer.Put(data);

            toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, byte[] rawPacketData)
        {
            var writer = new NetDataWriter();

            writer.Put(fromPeer.ConnectionId);
            writer.Put(rawPacketData);

            toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}
