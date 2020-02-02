using System.Threading;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Voyager
{
    internal static class HttpContextExtensions
    {
        public static CancellationToken GetCancellationToken(
            this HttpContext context)
        {
            return context.RequestAborted;
        }
    }
}
