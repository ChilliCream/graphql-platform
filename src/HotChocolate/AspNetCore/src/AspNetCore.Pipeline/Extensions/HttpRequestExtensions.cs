using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class HttpRequestExtensions
{
    private const string Slash = "/";
    private static readonly PathString s_slashPath = new("/");

    internal static bool IsEndpointPath(this HttpRequest request, PathString path)
    {
        var isBelowPath = request.Path.StartsWithSegments(
            path,
            StringComparison.OrdinalIgnoreCase,
            out var remaining);

        return isBelowPath && remaining.Value is null or "" or "/";
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
