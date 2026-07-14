using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Diagnostics.HotChocolateFusionActivitySource;

namespace HotChocolate.Fusion.Diagnostics.Listeners;

internal sealed class FusionActivityServerDiagnosticListener(
    FusionActivityEnricher enricher,
    InstrumentationOptions options)
    : ServerDiagnosticEventListener
{
    public override IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
    {
        if (options.SkipExecuteHttpRequest)
        {
            return EmptyScope;
        }

        var span = ExecuteHttpRequestSpan.Start(Source, context, kind, enricher, options);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void StartSingleRequest(HttpContext context, GraphQLRequest request)
    {
        if (options.IncludeRequestDetails
            && context.Features.Get<ExecuteHttpRequestSpan>() is { } span)
        {
            span.SetSingleRequestDetails(request);
        }
    }

    public override void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch)
    {
        if (options.IncludeRequestDetails
            && context.Features.Get<ExecuteHttpRequestSpan>() is { } span)
        {
            span.SetBatchRequestDetails(batch);
        }
    }

    public override void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
        if (options.IncludeRequestDetails
            && context.Features.Get<ExecuteHttpRequestSpan>() is { } span)
        {
            span.SetOperationBatchRequestDetails(request, operations);
        }
    }

    public override void HttpRequestError(HttpContext context, IError error)
    {
        if (context.Features.Get<ExecuteHttpRequestSpan>() is { } span)
        {
            span.RecordError(error);
        }
    }

    public override void HttpRequestError(HttpContext context, Exception exception)
    {
        if (context.Features.Get<ExecuteHttpRequestSpan>() is { } span)
        {
            span.RecordError(exception);
        }
    }

    public override IDisposable ParseHttpRequest(HttpContext context)
    {
        if (options.SkipParseHttpRequest)
        {
            return EmptyScope;
        }

        var span = ParseHttpRequestSpan.Start(Source, context, enricher);

        if (span is null)
        {
            return EmptyScope;
        }

        context.Features.Set(span);

        return span;
    }

    public override void ParserErrors(HttpContext context, IReadOnlyList<IError> errors)
    {
        if (context.Features.Get<ParseHttpRequestSpan>() is { } span)
        {
            span.RecordErrors(errors);
        }
    }

    public override IDisposable FormatHttpResponse(HttpContext context, OperationResult result)
    {
        if (options.SkipFormatHttpResponse)
        {
            return EmptyScope;
        }

        var span = FormatHttpResponseSpan.Start(Source, context, enricher);

        return span ?? EmptyScope;
    }
}
