using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("Nexus")]
    [JsonAlias("NexusDownloader, Wabbajack.Lib")]
    public class Nexus : IDownloadState
    {
        public string? Name { get; set; }

        public string? Author { get; set; }

        public string? Version { get; set; }

        public Uri? ImageURL { get; set; }

        public bool IsNSFW { get; set; }

        public string? Description { get; set; }

        [JsonPropertyName("GameName")] public Game Game { get; set; }
        public long ModID { get; set; }
        public long FileID { get; set; }

        public async Task<bool> LoadMetaData()
        {
            return true;
        }

        public object[] PrimaryKey => new object[] {Game, ModID, FileID};
    }
}