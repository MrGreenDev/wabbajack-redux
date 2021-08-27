using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CefNet;
using Microsoft.Extensions.Logging;
using Wabbajack.App.Extensions;
using Wabbajack.DTOs.Logins;
using Wabbajack.Services.OSIntegrated;

namespace Wabbajack.App.ViewModels
{
    public abstract class OAuthLoginViewModel : GuidedWebViewModel
    {
        private readonly HttpClient _httpClient;
        private readonly EncryptedJsonTokenProvider<OAuth2LoginState> _tokenProvider;

        public OAuthLoginViewModel(ILogger<OAuthLoginViewModel> logger, HttpClient httpClient, EncryptedJsonTokenProvider<OAuth2LoginState> tokenProvider) : base(logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;

        }

        protected abstract string SiteName { get; }
        protected abstract string[] Scopes { get; }

        protected abstract string ClientID { get; }

        protected abstract Uri AuthorizationEndpoint { get; }
        protected abstract Uri TokenEndpoint { get; }
        
        protected abstract string StorageKey { get; }


        private class AsyncSchemeHandler : CefSchemeHandlerFactory
        {
            private TaskCompletionSource<Uri> _tcs = new();
            public Task<Uri> Task => _tcs.Task;

            public AsyncSchemeHandler()
            {

            }

            protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName,
                CefRequest request)
            {
                return new Handler(_tcs);
            }
        }

        private class Handler : CefResourceHandler
        {
            private readonly TaskCompletionSource<Uri> _tcs;

            public Handler(TaskCompletionSource<Uri> tcs)
            {
                _tcs = tcs;
            }

            protected override bool ProcessRequest(CefRequest request, CefCallback callback)
            {
                _tcs.TrySetResult(new Uri(request.Url));
                return false;
            }
        }

        public override async Task Run(CancellationToken token)
        {
            await Browser.WaitForReady();

            var handler = new AsyncSchemeHandler();
            Browser.RequestContext.RegisterSchemeHandlerFactory("wabbajack", "", handler);

            Instructions = $"Please log in and allow Wabbajack to access your {SiteName} account";

            var scopes = string.Join(" ", Scopes);
            var state = Guid.NewGuid().ToString();

            await Browser.NavigateTo(new Uri(AuthorizationEndpoint +
                                             $"?response_type=code&client_id={ClientID}&state={state}&scope={scopes}"));

            var uri = await handler.Task.WaitAsync(token);

            var cookies = await Browser.Cookies("loverslab.com", token);

            var parsed = HttpUtility.ParseQueryString(uri.Query);
            if (parsed.Get("state") != state)
            {
                _logger.LogCritical("Bad OAuth state, this shouldn't happen");
                throw new Exception("Bad OAuth State");
            }

            if (parsed.Get("code") == null)
            {
                _logger.LogCritical("Bad code result from OAuth");
                throw new Exception("Bad code result from OAuth");
            }

            var authCode = parsed.Get("code");

            var formData = new KeyValuePair<string?, string?>[]
            {
                new("grant_type", "authorization_code"),
                new("code", authCode),
                new("client_id", ClientID)
            };

            var msg = new HttpRequestMessage();
            msg.Method = HttpMethod.Post;
            msg.RequestUri = TokenEndpoint;
            msg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
            msg.Headers.Add("Cookie", string.Join(";", cookies.Select(c => $"{c.Name}={c.Value}")));
            msg.Content = new FormUrlEncodedContent(formData.ToList());
                
            using var response = await _httpClient.SendAsync(msg, token);
            var data = await response.Content.ReadFromJsonAsync<OAuthResultState>(cancellationToken: token);

            await _tokenProvider.SetToken(new OAuth2LoginState
            {
                Cookies = cookies,
                ResultState = data!
            });

        }

       
    }


}