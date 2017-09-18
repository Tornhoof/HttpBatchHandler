namespace HttpBatchHandler
{
    public static class HttpApplicationMultipartExtensions
    {
        
        public static bool IsSuccessStatusCode(this HttpApplicationMultipart multipart) => multipart.StatusCode >= 200 && multipart.StatusCode <= 299;
    }
}