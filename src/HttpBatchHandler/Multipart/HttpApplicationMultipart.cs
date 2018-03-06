using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationMultipart : IMultipart
    {
        private static readonly char[] Crlf = "\r\n".ToCharArray();
        private readonly Stream _content;
        private readonly string _httpVersion;
        private readonly string _reasonPhrase;

        internal HttpApplicationMultipart(ResponseFeature responseFeature) : this(responseFeature.Protocol,
            responseFeature.StatusCode, responseFeature.ReasonPhrase, responseFeature.Body, responseFeature.Headers)
        {
        }

        public HttpApplicationMultipart(string httpVersion, int statusCode, string reasonPhrase, Stream content,
            IHeaderDictionary headers)
        {
            _httpVersion = httpVersion;
            StatusCode = statusCode;
            _reasonPhrase = reasonPhrase;
            if (string.IsNullOrEmpty(_reasonPhrase))
            {
                _reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);
            }

            _content = content;
            Headers = headers;
        }

        public IHeaderDictionary Headers { get; }
        public int StatusCode { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using (var sb = new StreamWriter(stream, Encoding.ASCII, 8192, true))
            {
                await sb.WriteAsync("Content-Type: application/http; msgtype=response").ConfigureAwait(false);
                await sb.WriteAsync(Crlf).ConfigureAwait(false);
                await sb.WriteAsync(Crlf).ConfigureAwait(false);
                await sb.WriteAsync(_httpVersion).ConfigureAwait(false);
                await sb.WriteAsync(' ').ConfigureAwait(false);
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                await sb.WriteAsync(StatusCode.ToString()).ConfigureAwait(false);
                await sb.WriteAsync(' ').ConfigureAwait(false);
                await sb.WriteAsync(_reasonPhrase).ConfigureAwait(false);
                await sb.WriteAsync(Crlf).ConfigureAwait(false);
                foreach (var header in Headers)
                {
                    await sb.WriteAsync(header.Key).ConfigureAwait(false);
                    await sb.WriteAsync(": ").ConfigureAwait(false);
                    await sb.WriteAsync(header.Value).ConfigureAwait(false);
                    await sb.WriteAsync(Crlf).ConfigureAwait(false);
                }

                await sb.WriteAsync(Crlf).ConfigureAwait(false);
            }

            if (_content != null)
            {
                await _content.CopyToAsync(stream).ConfigureAwait(false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
        }

        ~HttpApplicationMultipart()
        {
            Dispose(false);
        }
    }
}