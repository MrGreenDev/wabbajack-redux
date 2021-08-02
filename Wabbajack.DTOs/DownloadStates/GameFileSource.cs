using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("GameFileSource")]
    [JsonAlias("GameFileSourceDownloader, Wabbajack.Lib")]
    public class GameFileSource : IDownloadState
    {
        public Game Game { get; set; }
        public RelativePath GameFile { get; set; }
        public Hash Hash { get; set; }
        public string GameVersion { get; set; } = "";
        public object[] PrimaryKey => new object[] {Game, GameVersion ?? "0.0.0.0", GameFile.ToString().ToLowerInvariant()};
    }
}