using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Events
{
    public class BatchRequestPreparationContext
    {
        /// <summary>
        ///     Features which should be in the httpContext
        /// </summary>
        public IFeatureCollection Features { get; set; }

        /// <summary>
        ///     The individual request, prior to context creation
        /// </summary>
        public IHttpRequestFeature RequestFeature { get; set; }

        /// <summary>
        ///     State
        /// </summary>
        public object State { get; set; }
    }
}