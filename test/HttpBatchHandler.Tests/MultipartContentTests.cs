using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpBatchHandler.Multipart;
using Xunit;

namespace HttpBatchHandler.Tests
{
    public class MultipartContentTests
    {
        [Fact]
        public async Task CompareExample()
        {
            var mw = new MultipartContent("mixed", "batch_357647d1-a6b5-4e6a-aa73-edfc88d8866e")
            {
                new HttpApplicationContent(new HttpRequestMessage(HttpMethod.Get,
                    "http://localhost:12345/api/WebCustomers?Query=Parts")),
                new HttpApplicationContent(
                    new HttpRequestMessage(HttpMethod.Post, "http://localhost:12345/api/WebCustomers")
                    {
                        Content = new StringContent(
                            "{\"Id\":129,\"Name\":\"Name4752cbf0-e365-43c3-aa8d-1bbc8429dbf8\"}", Encoding.UTF8,
                            "application/json")
                    }),
                new HttpApplicationContent(
                    new HttpRequestMessage(HttpMethod.Put, "http://localhost:12345/api/WebCustomers/1")
                    {
                        Content = new StringContent("{\"Id\":1,\"Name\":\"Peter\"}", Encoding.UTF8, "application/json")
                    }),
                new HttpApplicationContent(new HttpRequestMessage(HttpMethod.Delete,
                    "http://localhost:12345/api/WebCustomers/2"))
            };
            string output;
            using (var memoryStream = new MemoryStream())
            {
                await mw.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                output = Encoding.ASCII.GetString(memoryStream.ToArray());
            }
            string input;

            using (var refTextStream = GetType().Assembly
                .GetManifestResourceStream(typeof(MultipartParserTests), "MultipartRequest.txt"))
            {
                Assert.NotNull(refTextStream);
                input = await refTextStream.ReadAsStringAsync().ConfigureAwait(false);
            }
            Assert.Equal(input, output);
        }
    }
}