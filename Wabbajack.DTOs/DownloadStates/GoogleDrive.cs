using System.Text.Json.Serialization;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("GoogleDrive")]
    [JsonAlias("GoogleDriveDownloader, Wabbajack.Lib")]
    public class GoogleDrive : IDownloadState
    {
        public string Id { get; init; }

        [JsonIgnore] 
        public object[] PrimaryKey => new object[] {Id};
    }
}