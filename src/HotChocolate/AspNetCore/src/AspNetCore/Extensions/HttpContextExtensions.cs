using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class HttpContextExtensions
{
    public static GraphQLServerOptions? GetGraphQLServerOptions(this HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<GraphQLServerOptions>() ??
           (context.Items.TryGetValue(nameof(GraphQLServerOptions), out var o) &&
            o is GraphQLServerOptions options
                ? options
                : null);

    public static GraphQLSocketOptions? GetGraphQLSocketOptions(this HttpContext context)
        => GetGraphQLServerOptions(context)?.Sockets;

    public static bool IncludeQueryPlan(this HttpContext context)
    {
        var headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.QueryPlan, out var values) &&
            values.Any(v => v == HttpHeaderValues.IncludeQueryPlan))
        {
            return true;
        }

        return false;
    }

    public static RequestContentType ParseContentType(this HttpContext context)
    {
        if (context.Items.TryGetValue(nameof(RequestContentType), out var value) &&
            value is RequestContentType contentType)
        {
            return contentType;
        }

        var span = context.Request.ContentType.AsSpan();

        if (span.StartsWith(ContentType.JsonSpan()))
        {
            context.Items[nameof(RequestContentType)] = RequestContentType.Json;
            return RequestContentType.Json;
        }

        if (span.StartsWith(ContentType.MultiPartFormSpan()))
        {
            context.Items[nameof(RequestContentType)] = RequestContentType.Form;
            return RequestContentType.Form;
        }

        context.Items[nameof(RequestContentType)] = RequestContentType.None;
        return RequestContentType.None;
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

            if(value.Equals(HttpHeaderValues.ReportCost, StringComparison.OrdinalIgnoreCase))
            {
                return WellKnownContextData.ReportCost;
            }

            if(value.Equals(HttpHeaderValues.ValidateCost, StringComparison.OrdinalIgnoreCase))
            {
                return WellKnownContextData.ValidateCost;
            }
        }

        return null;
    }
}
