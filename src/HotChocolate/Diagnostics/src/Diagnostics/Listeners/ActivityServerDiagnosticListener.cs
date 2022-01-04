using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Diagnostics.ContextKeys;

namespace HotChocolate.Diagnostics;

internal sealed class ActivityServerDiagnosticListener : ServerDiagnosticEventListener
{
    private readonly InstrumentationOptions _options;
    private readonly ActivityEnricher _enricher;

    public ActivityServerDiagnosticListener(
        ActivityEnricher enricher,
        InstrumentationOptions options)
    {
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
    {
        Activity? activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.Items[HttpRequestActivity] = activity;
        _enricher.EnrichExecuteHttpRequest(context, kind, activity);

        return activity;
    }

    public override void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
        if (_options.IncludeRequestDetails)
        {
            if (context.Items.TryGetValue(HttpRequestActivity, out var activity))
            {
                _enricher.EnrichSingleRequest(context, request, (Activity)activity!);
            }
        }
    }

    public override void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
    }

    public override void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
    }

    public override void HttpRequestError(HttpContext context, IError error)
    {

    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {

    }

    public override IDisposable ParseHttpRequest(HttpContext context)
    {
        Activity? activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.Items[ParseHttpRequestActivity] = activity;

        return activity;
    }

    public override void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {

    }

    public override IDisposable FormatHttpResponse(HttpContext context, IQueryResult result)
    {
        Activity? activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.Items[FormatHttpResponseActivity] = activity;

        return activity;
    }

    public override IDisposable WebSocketSession(HttpContext context)
    {
        Activity? activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        context.Items[WebSocketSessionActivity] = activity;

        return activity;
    }

    public override void WebSocketSessionError(HttpContext context, Exception exception)
    {

    }
}
