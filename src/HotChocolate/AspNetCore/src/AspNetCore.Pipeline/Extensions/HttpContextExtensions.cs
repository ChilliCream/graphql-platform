using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class HttpContextExtensions
{
    public static bool IncludeOperationPlan(this HttpContext context)
    {
        var headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.OperationPlan, out var values))
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == HttpHeaderValues.IncludeOperationPlan)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string? TryGetCostSwitch(this HttpContext context)
    {
        var headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.Cost, out var values))
        {
            if (values.Count == 0)
            {
                return null;
            }

            var value = values[0];

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

        if (span.StartsWith(ContentType.JsonSpan()))
        {
            return RequestContentType.Json;
        }

        if (span.StartsWith(ContentType.MultiPartFormSpan()))
        {
            return RequestContentType.Form;
        }

        return RequestContentType.None;
    }
}
