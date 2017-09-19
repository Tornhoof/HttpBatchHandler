using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationResponseSection
    {
        public IHttpResponseFeature ResponseFeature { get; set; }
    }

    public static class HttpApplicationResponseSectionExtensions
    {
        public static async Task<HttpApplicationResponseSection> ReadNextHttpApplicationResponseSectionAsync(
            this MultipartReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            return null;
        }


        public static async Task<string> ReadAsStringAsync(this HttpApplicationResponseSection section, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (section.ResponseFeature?.Body == null)
            {
                return null;
            }
            using (var tr = new StreamReader(section.ResponseFeature.Body))
            {
                return await tr.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
