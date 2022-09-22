using LiteNetLib.Utils;
using System;

namespace ChroMapTogether.UDP.Packets
{
    public class MapDataPacket : INetSerializable
    {
        public byte[] ZipBytes = Array.Empty<byte>();
        public string Characteristic = string.Empty;
        public string Diff = string.Empty;

        public MapDataPacket() { }

        public MapDataPacket(byte[] zipBytes, string characteristic, string diff)
        {
            ZipBytes = zipBytes;
            Characteristic = characteristic;
            Diff = diff;
        }

        public void Deserialize(NetDataReader reader)
        {
            Characteristic = reader.GetString();
            Diff = reader.GetString();
            ZipBytes = reader.GetBytesWithLength();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Characteristic);
            writer.Put(Diff);
            writer.PutBytesWithLength(ZipBytes);
        }
    }

}
