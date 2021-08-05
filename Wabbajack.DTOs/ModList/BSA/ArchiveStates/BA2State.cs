using System.Threading.Tasks;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.BSA.ArchiveStates
{
    public enum BA2EntryType
    {
        GNRL,
        DX10,
        GNMF
    }
    
    [JsonName("BA2State")]
    public class BA2State : IArchive
    {
        public bool HasNameTable { get; set; }
        public BA2EntryType Type { get; set; }
        public string HeaderMagic { get; set; }
        public uint Version { get; set; }
    }
}