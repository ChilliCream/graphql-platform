using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.ContextKeys;

namespace HotChocolate.Diagnostics.Listeners;

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
        if (_options.SkipExecuteHttpRequest)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichExecuteHttpRequest(context, kind, activity);
        activity.SetStatus(ActivityStatusCode.Ok);
        context.Items[HttpRequestActivity] = activity;

        return activity;
    }

    public override void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
        if (_options.IncludeRequestDetails &&
            context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            _enricher.EnrichSingleRequest(context, request, (Activity)activity!);
        }
    }

    public override void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
        if (_options.IncludeRequestDetails &&
            context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            _enricher.EnrichBatchRequest(context, batch, (Activity)activity!);
        }
    }

    public override void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
        if (_options.IncludeRequestDetails &&
            context.Items.TryGetValue(HttpRequestActivity, out var activity))
        {
            _enricher.EnrichOperationBatchRequest(
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
            _enricher.EnrichHttpRequestError(context, error, activity);
            activity.SetStatus(Status.Error);
        }
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        if (context.Items.TryGetValue(HttpRequestActivity, out var value))
        {
            var activity = (Activity)value!;
            _enricher.EnrichHttpRequestError(context, exception, activity);
            activity.SetStatus(Status.Error);
        }
    }

    public override IDisposable ParseHttpRequest(HttpContext context)
    {
        if (_options.SkipParseHttpRequest)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichParseHttpRequest(context, activity);
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
                _enricher.EnrichParserErrors(context, error, activity);
            }

            activity.SetStatus(Status.Error);
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    public override IDisposable FormatHttpResponse(HttpContext context, IOperationResult result)
    {
        if (_options.SkipFormatHttpResponse)
        {
            return EmptyScope;
        }

        var activity = HotChocolateActivitySource.Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        _enricher.EnrichFromatHttpResponse(context, activity);
        activity.SetStatus(ActivityStatusCode.Ok);
        context.Items[FormatHttpResponseActivity] = activity;

        return activity;
    }

    // removed for 12.5 public override IDisposable WebSocketSession(HttpContext context)
    // removed for 12.5public override void WebSocketSessionError(
    // HttpContext context, Exception exception)
}
