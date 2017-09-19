using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationContent : HttpContent
    {
        private readonly HttpRequestMessage _requestMessage;

        public HttpApplicationContent(HttpRequestMessage requestMessage)
        {
            _requestMessage = requestMessage;
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
