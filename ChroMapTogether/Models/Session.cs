using ChroMapTogether.UDP.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

namespace ChroMapTogether.Models
{
    public class Session
    {
        public Guid Guid { get; set; }
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Code { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public DateTime Expiry { get; set; } = DateTime.MaxValue;

        public NetPeer? Host { get; set; } = null;
        public List<NetPeer> ConnectedClients { get; set; } = new();
        public List<MapperIdentityPacket> Identities { get; set; } = new();
        public Dictionary<NetPeer, MapperPosePacket> CachedPoses { get; set; } = new();
        public List<NetPeer> ClientsWaitingForMap { get; set; } = new();

        public void Close()
        {
            foreach (var peer in ConnectedClients)
            {
                var writer = new NetDataWriter();
                writer.Put("Server closed.");
                peer.Disconnect(writer);
            }

            ConnectedClients.Clear();
            Identities.Clear();
            CachedPoses.Clear();
            ClientsWaitingForMap.Clear();
        }

        public void KickUser(NetPeer peer, string? reason = null)
        {
            if (ConnectedClients.Contains(peer))
            {
                if (reason != null)
                {
                    var writer = new NetDataWriter();
                    writer.Put(reason);
                    peer.Disconnect(writer);
                }
                else
                {
                    peer.Disconnect();
                }
            }
        }
    }
}
