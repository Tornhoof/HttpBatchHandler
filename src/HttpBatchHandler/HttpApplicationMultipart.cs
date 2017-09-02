using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler
{
    public class HttpApplicationMultipart : IMultipart, IDisposable
    {
        private static readonly char[] Crlf = "\r\n".ToCharArray();
        private readonly Stream _content;
        private readonly IHeaderDictionary _headers;
        private readonly string _httpVersion;
        private readonly string _reasonPhrase;
        private readonly int _statusCode;


        public HttpApplicationMultipart(string httpVersion, int statusCode, string reasonPhrase, Stream content,
            IHeaderDictionary headers)
        {
            _httpVersion = httpVersion;
            _statusCode = statusCode;
            _reasonPhrase = reasonPhrase;
            _content = content;
            _headers = headers;
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var sb = new StreamWriter(stream, Encoding.ASCII, 8192, true))
            {
                await sb.WriteAsync("Content-Type: application/http; msgType=response");
                await sb.WriteAsync(Crlf);
                await sb.WriteAsync(Crlf);
                await sb.WriteAsync(_httpVersion);
                await sb.WriteAsync(' ');
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                await sb.WriteAsync(_statusCode.ToString());
                await sb.WriteAsync(' ');
                await sb.WriteAsync(_reasonPhrase);
                await sb.WriteAsync(Crlf);
                foreach (var header in _headers)
                {
                    await sb.WriteAsync(header.Key);
                    await sb.WriteAsync(": ");
                    await sb.WriteAsync(header.Value);
                    await sb.WriteAsync(Crlf);
                }
                await sb.WriteAsync(Crlf);
            }
            if (_content != null)
            {
                await _content.CopyToAsync(stream);
            }
        }

        ~HttpApplicationMultipart()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
