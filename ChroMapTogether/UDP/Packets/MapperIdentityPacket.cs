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

        public NetPeer? MapperPeer;

        public MapperIdentityPacket() { }

        public void Deserialize(NetDataReader reader)
        {
            ConnectionId = reader.GetInt();
            Name = reader.GetString();
            Color = new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            DiscordId = reader.GetLong();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ConnectionId);
            writer.Put(Name);
            writer.Put(Color.r);
            writer.Put(Color.g);
            writer.Put(Color.b);
            writer.Put(DiscordId);
        }

        public record MapperColor(float r, float g, float b);
    }
}
