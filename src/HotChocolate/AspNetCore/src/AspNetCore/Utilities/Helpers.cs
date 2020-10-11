using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore.Utilities
{
    internal static class Helpers
    {
        internal static bool AcceptHeaderContainsHtml(IHeaderDictionary headers)
        {
            return headers.TryGetValue(HeaderNames.Accept, out StringValues values) &&
                values.Count > 0 && values[0].Contains("text/html");
        }

        internal static bool IsGetOrHeadMethod(string method)
        {
            return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
        }

        internal static bool PathEndsInSlash(PathString path)
        {
            return path.Value.EndsWith("/", StringComparison.Ordinal);
        }

        internal static bool TryMatchPath(
            HttpContext context,
            PathString matchUrl,
            bool forDirectory,
            out PathString subpath)
        {
            var path = context.Request.Path;

            if (forDirectory && !PathEndsInSlash(path))
            {
                path += new PathString("/");
            }

            if (path.StartsWithSegments(matchUrl, out subpath))
            {
                return true;
            }
            return false;
        }
    }
}
