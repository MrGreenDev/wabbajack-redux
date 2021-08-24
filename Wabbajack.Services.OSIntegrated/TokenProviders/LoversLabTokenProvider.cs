using Microsoft.Extensions.Logging;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.DTOs.Logins;

namespace Wabbajack.Services.OSIntegrated.TokenProviders
{
    public class LoversLabTokenProvider : EncryptedJsonTokenProvider<OAuth2LoginState>
    {
        public LoversLabTokenProvider(ILogger<LoversLabTokenProvider> logger, DTOSerializer dtos) : base(logger, dtos, "lovers-lab")
        {
        }
    }
}