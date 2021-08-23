using System;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("Http")]
    [JsonAlias("HttpDownloader")]
    [JsonAlias("HttpDownloader, Wabbajack.Lib")]
    public class Http : ADownloadState
    {
        public Uri Url { get; init; }
        public string[] Headers { get; set; } = Array.Empty<string>();
        public override string TypeName => "HTTPDownloader+State";
        public override object[] PrimaryKey => new object[] { Url };
    }
}