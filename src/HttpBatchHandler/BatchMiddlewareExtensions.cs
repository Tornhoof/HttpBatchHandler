using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBatchHandler
{
    public static class BatchMiddlewareExtensions
    {
        public static IApplicationBuilder UseBatchMiddleware(this IApplicationBuilder builder, PathString match)
        {
            var factory = builder.ApplicationServices.GetRequiredService<IHttpContextFactory>();
            return builder.UseMiddleware<BatchMiddleware>(factory, match);
        }
    }
}