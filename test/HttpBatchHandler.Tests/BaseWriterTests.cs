﻿using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler.Tests
{
    public abstract class BaseWriterTests
    {
        internal ResponseFeature CreateFirstResponse()
        {
            var output =
                "[{\"Id\":1,\"Name\":\"Namefc4b8794-943b-487a-9049-a8559232b9dd\"},{\"Id\":2,\"Name\":\"Name244bbada-3e83-43c8-82f7-5b2c4d72f2ed\"},{\"Id\":3,\"Name\":\"Nameec11d080-7f2d-47df-a483-7ff251cdda7a\"},{\"Id\":4,\"Name\":\"Name14ff5a3d-ad92-41f6-b4f6-9b94622f4968\"},{\"Id\":5,\"Name\":\"Name00f9e4cc-673e-4139-ba30-bfc273844678\"},{\"Id\":6,\"Name\":\"Name01f6660c-d1de-4c05-8567-8ae2759c4117\"},{\"Id\":7,\"Name\":\"Name60030a17-6316-427c-a744-b2fff6d9fe11\"},{\"Id\":8,\"Name\":\"Namefa61eb4c-9f9e-47a2-8dc5-15d8afe33f2d\"},{\"Id\":9,\"Name\":\"Name9b680c10-1727-43f5-83cf-c8eda3a63790\"},{\"Id\":10,\"Name\":\"Name9e66d797-d3a9-44ec-814d-aecde8040ced\"}]";
            var dictionary = new HeaderDictionary {{HeaderNames.ContentType, "application/json; charset=utf-8"}};
            var response = new ResponseFeature("HTTP/1.1", 200, "OK",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }

        internal ResponseFeature CreateFourthResponse()
        {
            var dictionary = new HeaderDictionary();
            var response = new ResponseFeature("HTTP/1.1", 204, "No Content",
                Stream.Null, dictionary);
            return response;
        }

        internal ResponseFeature CreateInternalServerResponse()
        {
            var dictionary = new HeaderDictionary();
            var response = new ResponseFeature("HTTP/1.1", 500, "Internal Server Error",
                Stream.Null, dictionary);
            return response;
        }

        internal ResponseFeature CreateSecondResponse()
        {
            var dictionary = new HeaderDictionary
            {
                {HeaderNames.Location, "http://localhost:13245/api/ApiCustomers"},
                {HeaderNames.ContentType, "application/json; charset=utf-8"}
            };
            var output = "{\"Id\":21,\"Name\":\"Name4752cbf0-e365-43c3-aa8d-1bbc8429dbf8\"}";
            var response = new ResponseFeature("HTTP/1.1", 201, "Created",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }

        internal ResponseFeature CreateThirdResponse()
        {
            var output =
                "{\"Id\":1,\"Name\":\"Peter\"}";
            var dictionary = new HeaderDictionary {{HeaderNames.ContentType, "application/json; charset=utf-8"}};
            var response = new ResponseFeature("HTTP/1.1", 200, "OK",
                new MemoryStream(Encoding.ASCII.GetBytes(output)), dictionary);
            return response;
        }
    }
}