using System;

namespace Wabbajack.DTOs.DownloadStates
{
    public abstract class IPS4OAuth2 : IDownloadState, IMetaState
    {
        public long IPS4Mod { get; set; }
            
        public bool IsAttachment { get; set; } = false;
        public string IPS4File { get; set; } = "";
        public string IPS4Url { get; set; } = "";
        
        public object[] PrimaryKey { get; }
        public Uri URL { get; }
        public string? Name { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public Uri? ImageURL { get; set; }
        public bool IsNSFW { get; set; }
        public string? Description { get; set; }
    }
}