using Microsoft.AspNetCore.Http;
using static System.Globalization.CultureInfo;
using static System.String;

namespace HotChocolate.AspNetCore;

internal static class HttpResponseExtensions
{
    private const string ContentDispositionHeader = "Content-Disposition";
    private const string ContentDispositionValue = "attachment; filename=\"{0}\"";

    public static IHeaderDictionary SetContentDisposition(
        this IHeaderDictionary headers,
        string fileName)
    {
        headers[ContentDispositionHeader] =
            Format(InvariantCulture, ContentDispositionValue, fileName);
        return headers;
    }
}
