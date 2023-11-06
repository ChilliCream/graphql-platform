using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AspNetCore;

internal static class HttpRequestExtensions
{
    private const string _slash = "/";
    private static readonly PathString _slashPath = new("/");

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
        return request.Path.Value?.EndsWith(_slash, StringComparison.Ordinal) ?? false;
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
            path += _slashPath;
        }

        if (path.StartsWithSegments(matchUrl, out subPath))
        {
            if (subPath.Value?.Length is 1 && subPath.Equals(_slashPath))
            {
                subPath = default;
            }
            return true;
        }

        return false;
    }
}
