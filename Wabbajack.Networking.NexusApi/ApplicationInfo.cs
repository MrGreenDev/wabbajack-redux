using System;

namespace Wabbajack.Networking.NexusApi
{
    public class ApplicationInfo
    {
        public string AppName { get; set; } = "";
        public Version AppVersion { get; set; } = new(1, 0, 0, 0);
    }
}