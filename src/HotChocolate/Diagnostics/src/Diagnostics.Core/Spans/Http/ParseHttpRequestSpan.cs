using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics;

internal sealed class ParseHttpRequestSpan(
    Activity activity,
    HttpContext httpContext,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ParseHttpRequestSpan? Start(
        ActivitySource source,
        HttpContext httpContext,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("Parse HTTP Request");

        if (activity is null)
        {
            return null;
        }

        activity.MarkAsSuccess();

        return new ParseHttpRequestSpan(activity, httpContext, enricher);
    }

    public void RecordErrors(IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            Activity.RecordError(error);
            enricher.EnrichError(Activity, error);
        }

        Activity.MarkAsError();
        enricher.EnrichParserErrors(Activity, httpContext, errors);
    }

    protected override void OnComplete()
    {
        enricher.EnrichParseHttpRequest(Activity, httpContext);
    }
}
