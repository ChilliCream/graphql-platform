using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class HttpContextExtensions
{
    public static bool IncludeOperationPlan(this HttpContext context)
    {
        var headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.OperationPlan, out var values)
            && values.Any(v => v == HttpHeaderValues.IncludeOperationPlan))
        {
            return true;
        }

        return false;
    }

    public static string? TryGetCostSwitch(this HttpContext context)
    {
        var headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.Cost, out var values))
        {
            var value = values.FirstOrDefault();

            if (value is null)
            {
                return null;
            }

            if (value.Equals(HttpHeaderValues.ReportCost, StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionContextData.ReportCost;
            }

            if (value.Equals(HttpHeaderValues.ValidateCost, StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionContextData.ValidateCost;
            }
        }

        return null;
    }

    public static RequestContentType ParseContentType(this HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(RequestContentType), out var value)
            && value is RequestContentType contentType)
        {
            return contentType;
        }

        var span = context.Request.ContentType.AsSpan();

        if (IsMediaType(span, ContentType.JsonSpan()))
        {
            return RequestContentType.Json;
        }

        if (IsMediaType(span, ContentType.MultiPartFormSpan()))
        {
            return RequestContentType.Form;
        }

        return RequestContentType.None;
    }

    /// <summary>
    /// Matches a known media type against a <c>Content-Type</c> value. Media types are
    /// case-insensitive per RFC 9110, section 8.3.1, and section 8.3 lets one be followed by
    /// parameters, so a match requires the value to end at the media type or to continue with
    /// the optional whitespace and semicolon that introduce them. Neither a longer subtype such
    /// as <c>application/json-patch+json</c> nor a malformed value such as
    /// <c>application/json garbage</c> is a match.
    /// </summary>
    /// <remarks>
    /// These are the semantics of <c>Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse</c>
    /// followed by <c>MatchesMediaType</c>, reproduced here because that pair allocates a header
    /// object and its parameter collection on a path that runs for every request.
    /// <c>HttpRequest.HasJsonContentType</c> is not an equivalent: it additionally accepts any
    /// <c>+json</c> structured suffix, and so matches <c>application/json-patch+json</c>.
    /// </remarks>
    private static bool IsMediaType(ReadOnlySpan<char> value, ReadOnlySpan<char> mediaType)
    {
        if (!value.StartsWith(mediaType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parameters = value[mediaType.Length..].TrimStart();

        return parameters.IsEmpty || parameters[0] is ';';
    }
}
