namespace ChroMapTogether.UDP.Packets
{
    public enum PacketId
    {
        MapperIdentity,
        MapperPose,

        SendZip,

        BeatmapObjectCreate,
        BeatmapObjectDelete,

        MapperDisconnect,
        MapperLatency,

        ActionCreated,
        ActionUndo,
        ActionRedo
    }

}
