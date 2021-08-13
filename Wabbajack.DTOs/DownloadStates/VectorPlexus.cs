using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.DownloadStates
{
    [JsonName("VectorPlexus")]
    [JsonAlias("VectorPlexusOAuthDownloader+State, Wabbajack.Lib")]
    public class VectorPlexus : IPS4OAuth2
    {
        public override string TypeName => "VectorPlexusOAuthDownloader+State";
    }
}