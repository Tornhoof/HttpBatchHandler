using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    public class MultipartWriter
    {
        private readonly byte[] _boundary;
        private readonly Stream _outputStream;

        public string ContentType { get; }

        public MultipartWriter(string subType, string boundary, Stream outputStream)
        {
            _boundary = Encoding.ASCII.GetBytes($"--{boundary}\r\n");
            _outputStream = outputStream;

            ContentType = $"multipart/{subType}; boundary=\"{boundary}\"";
        }

        public async Task WritePartAsync(IMultipart multipart,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _outputStream.WriteAsync(_boundary, 0, _boundary.Length, cancellationToken);
            await multipart.CopyToAsync(_outputStream, cancellationToken);
        }
    }
}
