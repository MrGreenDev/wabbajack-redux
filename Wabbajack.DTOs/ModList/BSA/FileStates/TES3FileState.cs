namespace Wabbajack.DTOs.BSA.FileStates
{
    public class TES3File : AFile
    {
        public uint Offset { get; set; }
        public uint NameOffset { get; set; }
        public uint Hash1 { get; set; }
        public uint Hash2 { get; set; }
        public uint Size { get; set; }
    }
}