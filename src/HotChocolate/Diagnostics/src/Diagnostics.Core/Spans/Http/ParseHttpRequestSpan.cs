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

        return new ParseHttpRequestSpan(activity, httpContext, enricher);
    }

    public void RecordErrors(IReadOnlyList<IError> errors)
    {
        Activity.SetStatus(ActivityStatusCode.Error);

        foreach (var error in errors)
        {
            Activity.AddGraphQLError(error);
        }

        enricher.EnrichParserErrors(httpContext, errors, Activity);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichParseHttpRequest(httpContext, Activity);
    }
}
