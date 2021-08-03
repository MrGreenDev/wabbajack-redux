using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Networking.Http
{
    public class ConnectionLimitingDelegate : DelegatingHandler
    {
        private readonly SemaphoreSlim _semaphore;

        public ConnectionLimitingDelegate(int connectionLimit, HttpMessageHandler inner) : base(inner)
        {
            _semaphore = new SemaphoreSlim(connectionLimit);
        }
        
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            var responseMessage = await base.SendAsync(request, cancellationToken);
            responseMessage.Content = new DisposingContent(responseMessage.Content, _semaphore);
            return responseMessage;
        }
        
        private class DisposingContent : HttpContent
        {
            private readonly HttpContent _inner;
            private readonly SemaphoreSlim _unlock;

            public DisposingContent(HttpContent inner, SemaphoreSlim unlock)
            {
                _inner = inner;
                _unlock = unlock;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                return _inner.CopyToAsync(stream, context);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _inner.Headers.ContentLength ?? 0;
                return _inner.Headers.ContentLength != null;
            }

            protected override void Dispose(bool disposing)
            {
                _inner.Dispose();
                _unlock.Release();
            }
        }
    }
}