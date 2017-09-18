using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HttpBatchHandler.Tests
{
    // https://blogs.msdn.microsoft.com/webdev/2013/11/01/introducing-batch-support-in-web-api-and-web-api-odata/
    public class MultipartWriterTests : BaseWriterTests
    {
        [Fact]
        public async Task CompareExample()
        {
            var mw = new MultipartWriter("mixed", "61cfbe41-7ea6-4771-b1c5-b43564208ee5");
            mw.Add(new HttpApplicationMultipart(CreateFirstResponse()));
            mw.Add(new HttpApplicationMultipart(CreateSecondResponse()));
            mw.Add(new HttpApplicationMultipart(CreateThirdResponse()));
            mw.Add(new HttpApplicationMultipart(CreateFourthResponse()));
            string output;
            using (var memoryStream = new MemoryStream())
            {
                await mw.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                output = Encoding.ASCII.GetString(memoryStream.ToArray());
            }
            string input;

            using (var refTextStream = GetType().Assembly
                .GetManifestResourceStream(typeof(MultipartParserTests), "MultipartResponse.txt"))
            {
                Assert.NotNull(refTextStream);
                using (var tr = new StreamReader(refTextStream))
                {
                    input = tr.ReadToEnd();
                }
            }
            Assert.Equal(input, output);
        }
    }
}