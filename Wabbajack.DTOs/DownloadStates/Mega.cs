using System;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("Mega")]
    [JsonAlias("MegaDownloader, Wabbajack.Lib")]
    public class Mega : IDownloadState
    {
        public Uri Url { get; init; }
        public object[] PrimaryKey => new object[] {Url};
    }
}