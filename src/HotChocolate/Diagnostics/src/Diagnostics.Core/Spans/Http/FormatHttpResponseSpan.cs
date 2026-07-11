using System.Diagnostics;
using Microsoft.AspNetCore.Http;

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

        return new FormatHttpResponseSpan(activity, httpContext, enricher);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichFormatHttpResponse(httpContext, Activity);
    }
}
