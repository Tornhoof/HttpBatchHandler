using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler
{
    public class HttpApplicationContent : HttpContent
    {
        private static readonly MediaTypeHeaderValue SContentType =
            MediaTypeHeaderValue.Parse("application/http; msgType=response");

        private static readonly char[] Crlf = "\r\n".ToCharArray();
        private readonly Stream _content;
        private readonly IHeaderDictionary _headers;
        private readonly string _httpVersion;
        private readonly string _reasonPhrase;
        private readonly int _statusCode;


        public HttpApplicationContent(string httpVersion, int statusCode, string reasonPhrase, Stream content,
            IHeaderDictionary headers)
        {
            _httpVersion = httpVersion;
            _statusCode = statusCode;
            _reasonPhrase = reasonPhrase;
            _content = content;
            _headers = headers;
            Headers.ContentType = SContentType;
        }

        private byte[] SerializeHeaders()
        {
            var sb = new StringBuilder();
            sb.Append(_httpVersion);
            sb.Append(' ');
            sb.Append(_statusCode);
            sb.Append(' ');
            sb.Append(_reasonPhrase);
            sb.Append(Crlf);
            foreach (var header in _headers)
            {
                sb.Append(header.Key);
                sb.Append(": ");
                sb.Append(header.Value);
                sb.Append(Crlf);
            }
            sb.Append(Crlf);
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var bytes = SerializeHeaders();
            await stream.WriteAsync(bytes, 0, bytes.Length);
            if (_content != null)
            {
                await _content.CopyToAsync(stream);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}