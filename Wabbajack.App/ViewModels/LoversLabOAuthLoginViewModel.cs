using System;
using System.Net.Http;
using CefNet;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs.Logins;
using Wabbajack.Services.OSIntegrated;
using Wabbajack.Services.OSIntegrated.TokenProviders;

namespace Wabbajack.App.ViewModels
{
    public class LoversLabOAuthLoginViewModel : OAuthLoginViewModel
    {
        public LoversLabOAuthLoginViewModel(ILogger<LoversLabOAuthLoginViewModel> logger, HttpClient client, 
            LoversLabTokenProvider tokenProvider)
            : base(logger, client, tokenProvider)
        {
        }

        protected override string SiteName => "Lovers Lab";
        protected override string[] Scopes => new[] { "downloads" };
        protected override string ClientID => "0b543a010bf1a8f0f4c5dae154fce7c3";
        protected override Uri AuthorizationEndpoint => new("https://loverslab.com/oauth/authorize/");
        protected override Uri TokenEndpoint => new("https://loverslab.com/oauth/token/");
        protected override string StorageKey => "lovers-lab";
    }
}