using System;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("Http")]
    [JsonAlias("HttpDownloader")]
    [JsonAlias("HttpDownloader, Wabbajack.Lib")]
    public class Http : IDownloadState
    {
        public Uri Url { get; init; }
        public string[] Headers { get; init; }
        public object[] PrimaryKey => new object[] {Url};
    }
}