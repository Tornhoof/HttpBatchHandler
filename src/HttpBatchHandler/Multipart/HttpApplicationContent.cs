using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationContent : HttpContent
    {
        private static readonly char[] Crlf = "\r\n".ToCharArray();
        private readonly HttpRequestMessage _message;

        public HttpApplicationContent(HttpRequestMessage message)
        {
            _message = message;
            Headers.ContentType = new MediaTypeHeaderValue("application/http");
            Headers.ContentType.Parameters.Add(new NameValueHeaderValue("msgtype","request"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _message.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using (var sb = new HttpResponseStreamWriter(stream, Encoding.ASCII))
            {
                await sb.WriteAsync(_message.Method.Method).ConfigureAwait(false);
                await sb.WriteAsync(' ').ConfigureAwait(false);
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                await sb.WriteAsync(_message.RequestUri.PathAndQuery).ConfigureAwait(false);
                await sb.WriteAsync(' ').ConfigureAwait(false);
                await sb.WriteAsync($"HTTP/{_message.Version}").ConfigureAwait(false);
                await sb.WriteAsync(Crlf).ConfigureAwait(false);
                await sb.WriteAsync($"Host: {_message.RequestUri.Authority}").ConfigureAwait(false);
                await sb.WriteAsync(Crlf).ConfigureAwait(false);
                foreach (var header in _message.Headers)
                {
                    await sb.WriteAsync(header.Key).ConfigureAwait(false);
                    await sb.WriteAsync(": ").ConfigureAwait(false);
                    await sb.WriteAsync(string.Join(", ", header.Value)).ConfigureAwait(false);
                    await sb.WriteAsync(Crlf).ConfigureAwait(false);
                }

                if (_message.Content?.Headers != null)
                {
                    foreach (var header in _message.Content?.Headers)
                    {
                        await sb.WriteAsync(header.Key).ConfigureAwait(false);
                        await sb.WriteAsync(": ").ConfigureAwait(false);
                        await sb.WriteAsync(string.Join(", ", header.Value)).ConfigureAwait(false);
                        await sb.WriteAsync(Crlf).ConfigureAwait(false);
                    }
                }

                await sb.WriteAsync(Crlf).ConfigureAwait(false);
            }

            if (_message.Content != null)
            {
                using (var contentStream = await _message.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    await contentStream.CopyToAsync(stream).ConfigureAwait(false);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}