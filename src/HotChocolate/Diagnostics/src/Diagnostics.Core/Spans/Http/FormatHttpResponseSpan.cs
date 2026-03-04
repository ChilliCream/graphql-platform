using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics;

internal sealed class FormatHttpResponseSpan(
    Activity activity,
    HttpContext httpContext,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static FormatHttpResponseSpan? Start(
        ActivitySource source,
        HttpContext httpContext,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("Format HTTP Response");

        if (activity is null)
        {
            return null;
        }

        activity.MarkAsSuccess();

        return new FormatHttpResponseSpan(activity, httpContext, enricher);
    }

    protected override void OnComplete()
    {
        enricher.EnrichFormatHttpResponse(Activity, httpContext);
    }
}
