using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace HttpBatchHandler.Tests
{
    public class MultipartParserTests
    {
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
            Assert.Collection(sections, InspectFirstRequest, InspectSecondRequest, InspectThirdRequest, InspectFourthRequest);
        }

        private void InspectFirstRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("GET", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
            Assert.Equal("?Query=Parts", obj.RequestFeature.QueryString);
        }

        private void InspectSecondRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("POST", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
        }


        private void InspectThirdRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("PUT", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/1", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
        }

        private void InspectFourthRequest(HttpApplicationRequestSection obj)
        {
            Assert.Equal("DELETE", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/2", obj.RequestFeature.Path);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal("http", obj.RequestFeature.Scheme);
        }
    }
}
