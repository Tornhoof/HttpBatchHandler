using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    public class MultipartWriter : IDisposable
    {
        private readonly byte[] _endBoundary;
        private readonly Queue<IMultipart> _parts = new Queue<IMultipart>();
        private readonly byte[] _startBoundary;

        public MultipartWriter(string subType, string boundary)
        {
            _startBoundary = Encoding.ASCII.GetBytes($"\r\n--{boundary}\r\n");
            _endBoundary = Encoding.ASCII.GetBytes($"\r\n--{boundary}--\r\n");

            ContentType = $"multipart/{subType}; boundary=\"{boundary}\"";
        }

        public string ContentType { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Add(IMultipart multipart)
        {
            _parts.Enqueue(multipart);
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (_parts.Count > 0)
            {
                using (var part = _parts.Dequeue())
                {
                    await stream.WriteAsync(_startBoundary, 0, _startBoundary.Length, cancellationToken);
                    await part.CopyToAsync(stream, cancellationToken);
                }
            }
            await stream.WriteAsync(_endBoundary, 0, _endBoundary.Length, cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_parts.Count > 0)
                {
                    var part = _parts.Dequeue();
                    part.Dispose();
                }
            }
        }

        ~MultipartWriter()
        {
            Dispose(false);
        }
    }
}