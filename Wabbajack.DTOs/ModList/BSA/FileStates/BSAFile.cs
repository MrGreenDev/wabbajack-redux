using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.BSA.FileStates
{
    [JsonName("BSAFile")]
    public class BSAFile : AFile
    {
        public bool FlipCompression { get; set; }
    }
}