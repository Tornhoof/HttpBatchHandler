using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace HttpBatchHandler.Tests
{
    // https://blogs.msdn.microsoft.com/webdev/2013/11/01/introducing-batch-support-in-web-api-and-web-api-odata/
    public class MultipartWriterTests : BaseWriterTests
    {
        private void ElementInspector(IHttpResponseFeature input, ResponseFeature comparison)
        {
            Assert.Equal(comparison.StatusCode, input.StatusCode);
            Assert.Equal(comparison.ReasonPhrase, input.ReasonPhrase);
            Assert.Equal(comparison.Headers, input.Headers);
        }

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

            using (var refTextStream = TestUtilities.GetNormalizedContentStream("MultipartResponse.txt"))
            {
                Assert.NotNull(refTextStream);
                input = await refTextStream.ReadAsStringAsync().ConfigureAwait(false);
            }
            Assert.Equal(input, output);
        }


        [Fact]
        public async Task ParseExample()
        {
            var reader = new MultipartReader("61cfbe41-7ea6-4771-b1c5-b43564208ee5",
                TestUtilities.GetNormalizedContentStream("MultipartResponse.txt"));
            var sections = new List<HttpApplicationResponseSection>();

            HttpApplicationResponseSection section;
            while ((section = await reader.ReadNextHttpApplicationResponseSectionAsync().ConfigureAwait(false)) != null)
            {
                sections.Add(section);
            }
            Assert.Equal(4, sections.Count);
            Assert.Collection(sections, x => ElementInspector(x.ResponseFeature, CreateFirstResponse()),
                x => ElementInspector(x.ResponseFeature, CreateSecondResponse()),
                x => ElementInspector(x.ResponseFeature, CreateThirdResponse()),
                x => ElementInspector(x.ResponseFeature, CreateFourthResponse()));
        }
    }
}