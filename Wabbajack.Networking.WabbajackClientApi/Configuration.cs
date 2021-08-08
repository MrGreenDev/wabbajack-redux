using System;

namespace Wabbajack.Networking.WabbajackClientApi
{
    public class Configuration
    {
        public Uri ServerUri { get; set; }
        public string MetricsKey { get; set; }
        public string MetricsKeyHeader { get; set; } = "x-metrics-key";
        public Uri ServerAllowList { get; set; } = new("https://raw.githubusercontent.com/wabbajack-tools/opt-out-lists/master/ServerWhitelist.yml");
        public Uri BuildServerUrl { get; set; } = new("https://build.wabbajack.org/");
    }
}