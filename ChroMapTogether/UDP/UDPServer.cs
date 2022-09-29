using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
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
        private readonly SessionRegistry sessionRegistry;
        private readonly IOptions<ServerConfiguration> serverConfig;
        private readonly Timer timer;

        private readonly Dictionary<NetPeer, Session> connectedSessions = new();

        public UDPServer(ILogger logger, SessionRegistry sessionRegistry, IOptions<ServerConfiguration> serverConfig)
        {
            this.logger = logger.ForContext<UDPServer>();
            this.sessionRegistry = sessionRegistry;
            this.serverConfig = serverConfig;

            eventBasedNetListener = new();
            eventBasedNetListener.ConnectionRequestEvent += EventBasedNetListener_ConnectionRequestEvent;
            eventBasedNetListener.PeerDisconnectedEvent += EventBasedNetListener_PeerDisconnectedEvent;
            eventBasedNetListener.NetworkReceiveEvent += EventBasedNetListener_NetworkReceiveEvent;
            eventBasedNetListener.NetworkLatencyUpdateEvent += EventBasedNetListener_NetworkLatencyUpdateEvent;

            NetDebug.Logger = this;

            netManager = new NetManager(eventBasedNetListener);
            netManager.Start(6969);

            timer = new Timer(1 / 60d * 1000);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            logger.Information("UDP server started.");
        }

        public void Dispose() => netManager.DisconnectAll();

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) => netManager.PollEvents();

        private void EventBasedNetListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            logger.Information("Got an incoming UDP connection");

            if (request.Data.TryGetString(out var roomCode)
                && roomCode.Length == serverConfig.Value.RoomCodeLength
                && sessionRegistry.TryGetSession(roomCode, out var session))
            {
                var peer = request.Accept();

                session.Host ??= peer;

                var identity = request.Data.Get<MapperIdentityPacket>();
                identity.ConnectionId = session.Identities.Count;
                identity.MapperPeer = peer;

                // Send peer identities and poses to new user
                foreach (var otherPeer in session.ConnectedClients)
                {
                    SendPacketFrom(identity, otherPeer, PacketId.MapperIdentity, identity);

                    var otherPeerIdentity = session.Identities.Find(it => it.MapperPeer == otherPeer);

                    if (otherPeerIdentity != null)
                    {
                        SendPacketFrom(otherPeerIdentity, peer, PacketId.MapperIdentity, otherPeerIdentity);

                        if (session.CachedPoses.TryGetValue(otherPeer, out var lastKnownPose))
                        {
                            SendPacketFrom(otherPeerIdentity, peer, PacketId.MapperPose, lastKnownPose);
                        }
                    }
                }

                if (session.Host != peer)
                {
                    SendPacketFrom(identity, session.Host, PacketId.CMT_IncomingMapper, new IncomingMapperPacket(peer.EndPoint.Address));
                }

                session.ConnectedClients.Add(peer);
                session.Identities.Add(identity);
                connectedSessions.Add(peer, session);

                logger.Information("Successfully established UDP connection.");
                return;
            }

            logger.Warning("UDP connection not related to a server; killing connection request...");

            request.Reject();
        }

        private void EventBasedNetListener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (connectedSessions.TryGetValue(peer, out var session))
            {
                // Bogus identity from the client
                var _ = reader.GetInt();

                // We need to correct identity with the connection ID from the peer
                var identity = session.Identities.Find(it => it.MapperPeer == peer);

                if (identity != null)
                {
                    var packetBytes = new byte[reader.AvailableBytes];
                    Array.Copy(reader.RawData, reader.Position, packetBytes, 0, reader.AvailableBytes);

                    var packetId = reader.GetByte();

                    if (packetId == (byte)PacketId.SendZip)
                    {
                        // Only forward zip packet to peers who need it
                        foreach (var otherPeer in session.ClientsWaitingForMap)
                        {
                            SendPacketFrom(identity, otherPeer, packetBytes);
                        }

                        session.ClientsWaitingForMap.Clear();
                        return;
                    }
                    // Cache pose in case new mappers connect
                    else if (packetId == (byte)PacketId.MapperPose)
                    {
                        session.CachedPoses[peer] = reader.Get<MapperPosePacket>();
                    }
                    // Kick user (only if host)
                    else if (packetId == (byte)PacketId.CMT_KickMapper && session.Host == peer)
                    {
                        var offendingId = reader.GetInt();
                        var offendingIdentity = session.Identities.Find(it => it.ConnectionId == offendingId);

                        if (offendingIdentity != null && offendingIdentity.MapperPeer != null)
                        {
                            if (reader.TryGetString(out var reason))
                            {
                                session.KickUser(offendingIdentity.MapperPeer, reason);
                            }
                            else
                            {
                                session.KickUser(offendingIdentity.MapperPeer, "Kicked by the host.");
                            }
                        }
                        return;
                    }
                    // Accept user (only if host)
                    else if (packetId == (byte)PacketId.CMT_AcceptMapper && session.Host == peer)
                    {
                        var acceptingId = reader.GetInt();
                        var acceptingIdentity = session.Identities.Find(it => it.ConnectionId == acceptingId);

                        if (acceptingIdentity != null && acceptingIdentity.MapperPeer != null)
                        {
                            // Request a zip file from the map host if the waitlist is empty.
                            if (session.ClientsWaitingForMap.Count == 0)
                            {
                                SendPacketTo(session.Host, PacketId.CMT_RequestZip);
                            }

                            session.ClientsWaitingForMap.Add(acceptingIdentity.MapperPeer);
                        }
                        return;
                    }

                    // Forward packet to other clients
                    foreach (var otherPeer in session.ConnectedClients)
                    {
                        if (otherPeer != peer)
                        {
                            SendPacketFrom(identity, otherPeer, packetBytes);
                        }
                    }
                }
            }
        }

        private void EventBasedNetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (connectedSessions.TryGetValue(peer, out var session))
            {
                // If the host is disconnected, kick everyone out
                if (session.Host == peer)
                {
                    sessionRegistry.DeleteSession(session);
                }
                else if (session.ConnectedClients.Remove(peer))
                {
                    var identity = session.Identities.Find(it => it.MapperPeer == peer);

                    if (identity != null)
                    {
                        identity.MapperPeer = null;

                        foreach (var otherPeer in session.ConnectedClients)
                        {
                            SendPacketFrom(identity, otherPeer, PacketId.MapperDisconnect);
                        }
                    }

                    session.CachedPoses.Remove(peer);
                    session.ClientsWaitingForMap.Remove(peer);
                }

                connectedSessions.Remove(peer);
            }
        }

        private void EventBasedNetListener_NetworkLatencyUpdateEvent(NetPeer peer, int latency)
        {
            if (connectedSessions.TryGetValue(peer, out var session))
            {
                var identity = session.Identities.Find(it => it.MapperPeer == peer);

                if (identity != null)
                {
                    foreach (var otherPeer in session.ConnectedClients)
                    {
                        if (otherPeer != peer)
                        {
                            SendPacketFrom(identity, otherPeer, PacketId.MapperLatency, new MapperLatencyPacket(latency));
                        }
                    }
                }
            }
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args) => logger.Information(str, args);
        
        public void SendPacketFrom(MapperIdentityPacket fromPeer, NetPeer toPeer, PacketId packetId)
        {
            var writer = new NetDataWriter();

            writer.Put(fromPeer.ConnectionId);
            writer.Put((byte)packetId);

            toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

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

        public void SendPacketTo(NetPeer toPeer, PacketId packetId)
        {
            var writer = new NetDataWriter();

            writer.Put(0);
            writer.Put((byte)packetId);

            toPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}
