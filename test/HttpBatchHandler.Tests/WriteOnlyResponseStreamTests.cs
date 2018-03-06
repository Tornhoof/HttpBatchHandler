using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace HttpBatchHandler.Tests
{
    public class WriteOnlyResponseStreamTests
    {
        private async Task<byte[]> GetBuffer(WriteOnlyResponseStream writeOnlyStream)
        {
            using (var ms = new MemoryStream())
            {
                await writeOnlyStream.CopyToAsync(ms).ConfigureAwait(false);
                return ms.ToArray();
            }
        }

        private void CompareStreams(MemoryStream ms1, MemoryStream ms2)
        {
            var md5 = MD5.Create();
            var h1 = md5.ComputeHash(ms1);
            var h2 = md5.ComputeHash(ms2);
            Assert.Equal(h1, h2);
        }

        [Fact]
        public async Task BeginEndWrite()
        {
            var isAborted = false;
            Action abortRequest = () => isAborted = true;
            using (var writeOnlyStream = new WriteOnlyResponseStream(abortRequest))
            {
                var buffer = new byte[] {1, 2, 3, 4, 5};
                var iar = writeOnlyStream.BeginWrite(buffer, 0, buffer.Length, null, null);
                var task = Task.Factory.FromAsync(iar, writeOnlyStream.EndWrite);
                await task.ConfigureAwait(false);
                var writtenBuffer = await GetBuffer(writeOnlyStream).ConfigureAwait(false);
                Assert.Equal(buffer, writtenBuffer);
            }

            Assert.True(isAborted);
        }

        [Fact]
        public void IsAborted()
        {
            var isAborted = false;
            Action abortRequest = () => isAborted = true;
            using (var writeOnlyStream = new WriteOnlyResponseStream(abortRequest))
            {
                writeOnlyStream.WriteByte(0);
            }

            Assert.True(isAborted);
        }

        [Fact]
        public async Task WriteAsync()
        {
            var isAborted = false;
            Action abortRequest = () => isAborted = true;
            using (var writeOnlyStream = new WriteOnlyResponseStream(abortRequest))
            {
                var buffer = new byte[] {1, 2, 3, 4, 5};
                await writeOnlyStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                var writtenBuffer = await GetBuffer(writeOnlyStream).ConfigureAwait(false);
                Assert.Equal(buffer, writtenBuffer);
            }

            Assert.True(isAborted);
        }

        [Fact]
        public async Task WriteRandom()
        {
            var isAborted = false;
            Action abortRequest = () => isAborted = true;
            using (var ms1 = new MemoryStream())
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var writeOnlyStream = new WriteOnlyResponseStream(abortRequest))
                    {
                        var random = new Random();
                        for (var i = 0; i < 10; i++)
                        {
                            var buffer = ArrayPool<byte>.Shared.Rent(random.Next(50000));
                            random.NextBytes(buffer);
                            await writeOnlyStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            await ms1.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            ArrayPool<byte>.Shared.Return(buffer);
                        }

                        await writeOnlyStream.CopyToAsync(ms2).ConfigureAwait(false);
                        ms1.Position = 0;
                        ms2.Position = 0;
                        CompareStreams(ms1, ms2);
                    }
                }
            }

            Assert.True(isAborted);
        }

        [Fact]
        public async Task WriteSync()
        {
            var isAborted = false;
            Action abortRequest = () => isAborted = true;
            using (var writeOnlyStream = new WriteOnlyResponseStream(abortRequest))
            {
                var buffer = new byte[] {1, 2, 3, 4, 5};
                writeOnlyStream.Write(buffer, 0, buffer.Length);
                var writtenBuffer = await GetBuffer(writeOnlyStream).ConfigureAwait(false);
                Assert.Equal(buffer, writtenBuffer);
            }

            Assert.True(isAborted);
        }
    }
}