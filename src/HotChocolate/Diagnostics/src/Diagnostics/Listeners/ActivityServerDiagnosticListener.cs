using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.ContextKeys;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityServerDiagnosticListener(
    ActivityEnricher enricher,
    InstrumentationOptions options)
    : ServerDiagnosticEventListener
{
    public override IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
    {
        if (options.SkipExecuteHttpRequest)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        enricher.EnrichExecuteHttpRequest(context, kind, activity);
        activity.SetStatus(ActivityStatusCode.Ok);
        context.Items[HttpRequestActivity] = activity;

        return activity;
    }

    public override void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
        if (options.IncludeRequestDetails
            && context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            enricher.EnrichSingleRequest(context, request, (Activity)activity!);
        }
    }

    public override void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
        if (options.IncludeRequestDetails
            && context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            enricher.EnrichBatchRequest(context, batch, (Activity)activity!);
        }
    }

    public override void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
        if (options.IncludeRequestDetails
            && context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            enricher.EnrichOperationBatchRequest(
                context,
                request,
                operations,
                (Activity)activity!);
        }
    }

    public override void HttpRequestError(HttpContext context, IError error)
    {
        if (context.Items.TryGetValue(HttpRequestActivity, out var value))
        {
            var activity = (Activity)value!;
            enricher.EnrichHttpRequestError(context, error, activity);
            activity.SetStatus(Status.Error);
        }
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        if (context.Items.TryGetValue(HttpRequestActivity, out var value))
        {
            var activity = (Activity)value!;
            enricher.EnrichHttpRequestError(context, exception, activity);
            activity.SetStatus(Status.Error);
        }
    }

    public override IDisposable ParseHttpRequest(HttpContext context)
    {
        if (options.SkipParseHttpRequest)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        enricher.EnrichParseHttpRequest(context, activity);
        activity.SetStatus(Status.Ok);
        activity.SetStatus(ActivityStatusCode.Ok);
        context.Items[ParseHttpRequestActivity] = activity;

        return activity;
    }

    public override void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {
        if (context.Items.TryGetValue(ParseHttpRequestActivity, out var value))
        {
            var activity = (Activity)value!;

            foreach (var error in errors)
            {
                enricher.EnrichParserErrors(context, error, activity);
            }

            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override IDisposable FormatHttpResponse(HttpContext context, OperationResult result)
    {
        if (options.SkipFormatHttpResponse)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        enricher.EnrichFormatHttpResponse(context, activity);
        activity.SetStatus(ActivityStatusCode.Ok);
        context.Items[FormatHttpResponseActivity] = activity;

        return activity;
    }
}
