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

        activity.SetStatus(ActivityStatusCode.Ok);

        return new ParseHttpRequestSpan(activity, httpContext, enricher);
    }

    public void RecordErrors(IReadOnlyList<IError> errors)
    {
        Activity.SetStatus(ActivityStatusCode.Error);

        foreach (var error in errors)
        {
            Activity.AddGraphQLError(error);
        }

        enricher.EnrichParserErrors(Activity, httpContext, errors);
    }

    protected override void OnComplete()
    {
        enricher.EnrichParseHttpRequest(Activity, httpContext);
    }
}
