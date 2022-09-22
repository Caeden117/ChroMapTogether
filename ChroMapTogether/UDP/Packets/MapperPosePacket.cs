using LiteNetLib.Utils;

namespace ChroMapTogether.UDP.Packets
{
    public class MapperPosePacket : INetSerializable
    {
        public Vector3 Position = new(0, 0, 0);
        public Quaternion Rotation = new(0, 0, 0, 0);
        public float SongPosition = 0;
        public bool IsPlayingSong = false;
        public float PlayingSongSpeed = 1f;

        public MapperPosePacket() { }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);
            writer.Put(SongPosition);
            writer.Put(IsPlayingSong);
            writer.Put(PlayingSongSpeed);
        }

        public void Deserialize(NetDataReader reader)
        {
            Position = new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            SongPosition = reader.GetFloat();
            IsPlayingSong = reader.GetBool();
            PlayingSongSpeed = reader.GetFloat();
        }
    
        public record Vector3(float X, float Y, float Z);
        public record Quaternion(float X, float Y, float Z, float W);
    }
}
