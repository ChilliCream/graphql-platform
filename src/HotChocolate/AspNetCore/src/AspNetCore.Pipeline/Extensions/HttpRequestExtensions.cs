using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore;

internal static class HttpRequestExtensions
{
    private const string Slash = "/";
    private static readonly PathString s_slashPath = new("/");

    internal static bool AcceptHeaderContainsHtml(this HttpRequest request)
    {
        return request.Headers.TryGetValue(HeaderNames.Accept, out var values) &&
            values.Count > 0 && (values[0]?.Contains(ContentType.Html) ?? false);
    }

    internal static bool IsGetOrHeadMethod(this HttpRequest request)
    {
        return HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
    }

    internal static bool PathEndsInSlash(this HttpRequest request)
    {
        return request.Path.Value?.EndsWith(Slash, StringComparison.Ordinal) ?? false;
    }

    internal static bool TryMatchPath(
        this HttpRequest request,
        PathString matchUrl,
        bool forDirectory,
        out PathString subPath)
    {
        var path = request.Path;

        if (forDirectory && !request.PathEndsInSlash())
        {
            path += s_slashPath;
        }

        if (path.StartsWithSegments(matchUrl, out subPath))
        {
            if (subPath.Value?.Length is 1 && subPath.Equals(s_slashPath))
            {
                subPath = default;
            }
            return true;
        }

        return false;
    }
}
