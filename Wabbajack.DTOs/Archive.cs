using Wabbajack.DTOs.DownloadStates;
using Wabbajack.Paths;
using Wabbajack.Hashing.xxHash64;

namespace Wabbajack.DTOs
{
    public class Archive
    {
        public Hash Hash { get; init; }
        public string Meta { get; init; } = "";
        public string Name { get; init; }
        public ulong Size { get; init; }
        public IDownloadState State { get; init; }
    }
}