using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

namespace ChroMapTogether.UDP.Packets
{
    public class IncomingMapperPacket : INetSerializable
    {
        public IPAddress Ip = IPAddress.Loopback;

        public IncomingMapperPacket() { }

        public IncomingMapperPacket(IPAddress address) => Ip = address;

        public void Serialize(NetDataWriter writer) => writer.Put(Ip.MapToIPv4().ToString());

        public void Deserialize(NetDataReader reader)
        {
            if (!IPAddress.TryParse(reader.GetString(), out Ip!))
            {
                throw new InvalidPacketException("Could not parse IP");
            }
        }
    }
}
