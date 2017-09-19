using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationContent : HttpContent
    {
        private static readonly char[] Crlf = "\r\n".ToCharArray();
        private readonly HttpRequestMessage _message;

        public HttpApplicationContent(HttpRequestMessage message)
        {
            _message = message;
            Headers.ContentType = MediaTypeHeaderValue.Parse("application/http; msgtype=request");
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
            using (var sb = new StreamWriter(stream, Encoding.ASCII, 8192, true))
            {
                await sb.WriteAsync(_message.Method.Method);
                await sb.WriteAsync(' ');
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                await sb.WriteAsync(_message.RequestUri.PathAndQuery);
                await sb.WriteAsync(' ');
                await sb.WriteAsync($"HTTP/{_message.Version}");
                await sb.WriteAsync(Crlf);
                await sb.WriteAsync($"Host: {_message.RequestUri.Authority}");
                await sb.WriteAsync(Crlf);
                foreach (var header in _message.Headers)
                {
                    await sb.WriteAsync(header.Key);
                    await sb.WriteAsync(": ");
                    await sb.WriteAsync(string.Join(", ", header.Value));
                    await sb.WriteAsync(Crlf);
                }
                if (_message.Content?.Headers != null)
                {
                    foreach (var header in _message.Content?.Headers)
                    {
                        await sb.WriteAsync(header.Key);
                        await sb.WriteAsync(": ");
                        await sb.WriteAsync(string.Join(", ", header.Value));
                        await sb.WriteAsync(Crlf);
                    }
                }
                await sb.WriteAsync(Crlf);
            }
            if (_message.Content != null)
            {
                using (var contentStream = await _message.Content.ReadAsStreamAsync())
                {
                    await contentStream.CopyToAsync(stream);
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