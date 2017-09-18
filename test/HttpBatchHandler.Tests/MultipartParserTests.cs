using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;

namespace HttpBatchHandler.Tests
{
    // https://blogs.msdn.microsoft.com/webdev/2013/11/01/introducing-batch-support-in-web-api-and-web-api-odata/
    public class MultipartParserTests
    {
        private void InspectFirstRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("GET", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
            Assert.Equal("?Query=Parts", obj.RequestFeature.QueryString);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
        }

        private void InspectSecondRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("POST", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
            var serializer = JsonSerializer.Create();
            dynamic deserialized =
                serializer.Deserialize(new JsonTextReader(new StreamReader(obj.RequestFeature.Body)));
            Assert.Equal("129", deserialized.Id.ToString());
            Assert.Equal("Name4752cbf0-e365-43c3-aa8d-1bbc8429dbf8", deserialized.Name.ToString());
        }


        private void InspectThirdRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("PUT", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/1", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
            var serializer = JsonSerializer.Create();
            dynamic deserialized =
                serializer.Deserialize(new JsonTextReader(new StreamReader(obj.RequestFeature.Body)));
            Assert.Equal("1", deserialized.Id.ToString());
            Assert.Equal("Peter", deserialized.Name.ToString());
        }

        private void InspectFourthRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("DELETE", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/2", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
        }

        [Fact]
        public async Task Parse()
        {
            var reader = new MultipartReader("batch_357647d1-a6b5-4e6a-aa73-edfc88d8866e",
                GetType().Assembly.GetManifestResourceStream(typeof(MultipartParserTests), "MultipartRequest.txt"));
            var sections = new List<HttpApplicationRequestSection>();

            HttpApplicationRequestSection section;
            while ((section = await reader.ReadNextHttpApplicationRequestSectionAsync()) != null)
            {
                sections.Add(section);
            }
            Assert.Equal(4, sections.Count);
            Assert.Collection(sections, InspectFirstRequest, InspectSecondRequest, InspectThirdRequest,
                InspectFourthRequest);
        }
    }
}