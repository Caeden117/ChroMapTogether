using LiteNetLib;
using LiteNetLib.Utils;

namespace ChroMapTogether.UDP.Packets
{
    public class MapperIdentityPacket : INetSerializable
    {
        public string Name = "Uninitialized Mapper";
        public int ConnectionId = int.MinValue;
        public MapperColor Color = new(0, 0, 0);
        public long DiscordId = -1;
        public string AppVersion = string.Empty;

        public NetPeer? MapperPeer;

        public MapperIdentityPacket() { }

        public void Deserialize(NetDataReader reader)
        {
            ConnectionId = reader.GetInt();
            Name = reader.GetString();
            Color = new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            DiscordId = reader.GetLong();

            // Stable CM currently doesn't send appVersion
            AppVersion = reader.TryGetString(out var version) ? version : ""; 
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ConnectionId);
            writer.Put(Name);
            writer.Put(Color.r);
            writer.Put(Color.g);
            writer.Put(Color.b);
            writer.Put(DiscordId);
            writer.Put(AppVersion);
        }

        public record MapperColor(float r, float g, float b);
    }
}
