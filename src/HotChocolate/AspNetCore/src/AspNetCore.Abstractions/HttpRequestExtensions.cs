using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public static class HttpRequestExtensions
    {
        public static bool IsHttps(
            this HttpRequest request)
        {
            return request.IsHttps;
        }
    }
}
