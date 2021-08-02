using System;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("WabbajackCDN")]
    [JsonAlias("WabbajackCDNDownloader+State, Wabbajack.Lib")]
    public class WabbajackCDN : IDownloadState
    {
        public Uri Url { get; init; }
        public object[] PrimaryKey => new object[] {Url};
    }
}