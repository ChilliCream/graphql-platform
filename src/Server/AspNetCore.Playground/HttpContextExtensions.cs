using System.Threading;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Playground
#else
namespace HotChocolate.AspNetCore.Playground
#endif
{
    internal static class HttpContextExtensions
    {
        public static CancellationToken GetCancellationToken(
            this HttpContext context)
        {
#if ASPNETCLASSIC
            return context.Request.CallCancelled;
#else
            return context.RequestAborted;
#endif
        }
    }
}
