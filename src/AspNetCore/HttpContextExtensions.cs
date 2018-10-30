using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class HttpContextExtensions
    {
        public static bool IsRouteValid(this HttpContext context, string route)
        {
            string path = context.Request.Path.ToUriComponent();
            return string.Equals(route, path, StringComparison.Ordinal);
        }
    }
}
