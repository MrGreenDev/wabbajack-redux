namespace Wabbajack.DTOs.BSA.ArchiveStates
{
    public class TES3State : IArchive
    {
        public uint FileCount { get; set; }
        public long DataOffset { get; set; }
        public uint HashOffset { get; set; }
        public uint VersionNumber { get; set; }
    }
}