using LiteNetLib.Utils;

namespace ChroMapTogether.UDP.Packets
{
    public class MapperLatencyPacket : INetSerializable
    {
        public int Latency = -1;

        public MapperLatencyPacket() { }

        public MapperLatencyPacket(int latency) => Latency = latency;

        public void Deserialize(NetDataReader reader) => Latency = reader.GetInt();

        public void Serialize(NetDataWriter writer) => writer.Put(Latency);
    }
}
