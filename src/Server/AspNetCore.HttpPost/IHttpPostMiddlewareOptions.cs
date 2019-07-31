#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public interface IHttpPostMiddlewareOptions
        : IPathOptionAccessor
        , IParserOptionsAccessor
    {
        int MaxRequestSize { get; }
    }
}
