using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace HttpBatchHandler.Tests
{
    // https://blogs.msdn.microsoft.com/webdev/2013/11/01/introducing-batch-support-in-web-api-and-web-api-odata/
    public class MultipartWriterTests
    {
        [Fact]
        public async Task CompareExample()
        {
            var mw = new MultipartWriter("mixed", "61cfbe41-7ea6-4771-b1c5-b43564208ee5");
            mw.Add(CreateFirstResponse());
            mw.Add(CreateSecondResponse());
            mw.Add(CreateThirdResponse());
            mw.Add(CreateFourthResponse());
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

        private HttpApplicationMultipart CreateFirstResponse()
        {
            var output =
                "[{\"Id\":1,\"Name\":\"Namefc4b8794-943b-487a-9049-a8559232b9dd\"},{\"Id\":2,\"Name\":\"Name244bbada-3e83-43c8-82f7-5b2c4d72f2ed\"},{\"Id\":3,\"Name\":\"Nameec11d080-7f2d-47df-a483-7ff251cdda7a\"},{\"Id\":4,\"Name\":\"Name14ff5a3d-ad92-41f6-b4f6-9b94622f4968\"},{\"Id\":5,\"Name\":\"Name00f9e4cc-673e-4139-ba30-bfc273844678\"},{\"Id\":6,\"Name\":\"Name01f6660c-d1de-4c05-8567-8ae2759c4117\"},{\"Id\":7,\"Name\":\"Name60030a17-6316-427c-a744-b2fff6d9fe11\"},{\"Id\":8,\"Name\":\"Namefa61eb4c-9f9e-47a2-8dc5-15d8afe33f2d\"},{\"Id\":9,\"Name\":\"Name9b680c10-1727-43f5-83cf-c8eda3a63790\"},{\"Id\":10,\"Name\":\"Name9e66d797-d3a9-44ec-814d-aecde8040ced\"}]";
            var dictionary = new HeaderDictionary {{HeaderNames.ContentType, "application/json; charset=utf-8"}};
            var response = new HttpApplicationMultipart("HTTP/1.1", 200, "OK",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }

        private HttpApplicationMultipart CreateSecondResponse()
        {
            var dictionary = new HeaderDictionary
            {
                {HeaderNames.Location, "http://localhost:13245/api/ApiCustomers"},
                {HeaderNames.ContentType, "application/json; charset=utf-8"}
            };
            var output = "{\"Id\":21,\"Name\":\"Name4752cbf0-e365-43c3-aa8d-1bbc8429dbf8\"}";
            var response = new HttpApplicationMultipart("HTTP/1.1", 201, "Created",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }

        private HttpApplicationMultipart CreateThirdResponse()
        {
            var output =
                "{\"Id\":1,\"Name\":\"Peter\"}";
            var dictionary = new HeaderDictionary { { HeaderNames.ContentType, "application/json; charset=utf-8" } };
            var response = new HttpApplicationMultipart("HTTP/1.1", 200, "OK",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }

        private HttpApplicationMultipart CreateFourthResponse()
        {
            var dictionary = new HeaderDictionary();
            var response = new HttpApplicationMultipart("HTTP/1.1", 204, "No Content",
                Stream.Null, dictionary);
            return response;
        }
    }
}
