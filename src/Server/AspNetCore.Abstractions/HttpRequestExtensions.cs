
#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpRequest = Microsoft.Owin.IOwinRequest;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public static class HttpRequestExtensions
    {
        public static bool IsHttps(
            this HttpRequest request)
        {
#if ASPNETCLASSIC
            return request.IsSecure;
#else
            return request.IsHttps;
#endif
        }
    }
}
