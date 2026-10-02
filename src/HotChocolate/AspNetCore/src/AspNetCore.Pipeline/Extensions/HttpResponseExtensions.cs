using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using static System.Globalization.CultureInfo;
using static System.String;

namespace HotChocolate.AspNetCore;

internal static class HttpResponseExtensions
{
    private const string ContentDispositionHeader = "Content-Disposition";
    private const string ContentDispositionValue = "attachment; filename=\"{0}\"";

    /// <summary>
    /// Adds each comma-separated field name in <paramref name="fieldNames"/> to the <c>Vary</c>
    /// header unless the header already lists it, compared case-insensitively, or lists <c>*</c>.
    /// </summary>
    public static void AppendVary(this IHeaderDictionary headers, string fieldNames)
    {
        var vary = headers.Vary;
        var remaining = fieldNames.AsSpan();
        var total = 0;
        var listed = 0;

        while (TryReadFieldName(ref remaining, out var fieldName))
        {
            total++;

            if (ListsFieldName(vary, fieldName))
            {
                listed++;
            }
        }

        if (listed == total)
        {
            return;
        }

        if (listed == 0)
        {
            headers.Vary = StringValues.Concat(vary, fieldNames);
            return;
        }

        var missing = new List<string>(total - listed);
        remaining = fieldNames.AsSpan();

        while (TryReadFieldName(ref remaining, out var fieldName))
        {
            if (!ListsFieldName(vary, fieldName))
            {
                missing.Add(fieldName.ToString());
            }
        }

        headers.Vary = StringValues.Concat(vary, Join(", ", missing));
    }

    public static IHeaderDictionary SetContentDisposition(
        this IHeaderDictionary headers,
        string fileName)
    {
        headers[ContentDispositionHeader] =
            Format(InvariantCulture, ContentDispositionValue, fileName);
        return headers;
    }

    private static bool ListsFieldName(StringValues values, ReadOnlySpan<char> fieldName)
    {
        foreach (var value in values)
        {
            var remaining = value.AsSpan();

            while (TryReadFieldName(ref remaining, out var listedFieldName))
            {
                if (listedFieldName is "*"
                    || listedFieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryReadFieldName(
        ref ReadOnlySpan<char> remaining,
        out ReadOnlySpan<char> fieldName)
    {
        while (!remaining.IsEmpty)
        {
            var comma = remaining.IndexOf(',');
            fieldName = (comma < 0 ? remaining : remaining[..comma]).Trim();
            remaining = comma < 0 ? [] : remaining[(comma + 1)..];

            if (!fieldName.IsEmpty)
            {
                return true;
            }
        }

        fieldName = default;
        return false;
    }
}
