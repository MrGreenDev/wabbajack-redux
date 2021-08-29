using System;
using System.Net.Http;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs.Logins;
using Wabbajack.Services.OSIntegrated;
using Wabbajack.Services.OSIntegrated.TokenProviders;

namespace Wabbajack.App.ViewModels
{
    public class VectorPlexusOAuthLoginViewModel : OAuthLoginViewModel<VectorPlexusLoginState>
    {
        public VectorPlexusOAuthLoginViewModel(ILogger<LoversLabOAuthLoginViewModel> logger, HttpClient client, 
            VectorPlexusTokenProvider tokenProvider)
            : base(logger, client, tokenProvider)
        {
        }

        protected override string SiteName => "Vector Plexus";
        protected override string[] Scopes => new[] { "profile", "get_downloads" };
        protected override string ClientID => "45c6d3c9867903a7daa6ded0a38cedf8";
        protected override Uri AuthorizationEndpoint => new("https://vectorplexus.com/oauth/authorize/");
        protected override Uri TokenEndpoint => new("https://vectorplexus.com/oauth/token/");
        protected override string StorageKey => "vector-plexus";
    }
}