using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IPreparedOperationCache _operationCache;

    private OperationCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IPreparedOperationCache operationCache)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(operationCache);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _operationCache = operationCache;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentId = context.GetOperationDocumentId();

        if (documentId.IsEmpty)
        {
            await _next(context).ConfigureAwait(false);
        }
        else
        {
            var addToCache = true;
            var operationId = context.GetOperationId() ?? context.CreateCacheId();

            if (_operationCache.TryGetOperation(operationId, out var operation))
            {
                context.SetOperation(operation);
                addToCache = false;
                _diagnosticEvents.RetrievedOperationFromCache(context);
            }

            await _next(context).ConfigureAwait(false);

            if (addToCache && context.TryGetOperation(out operation))
            {
                _operationCache.TryAddOperation(operation.Id, operation);
                _diagnosticEvents.AddedOperationToCache(context);
            }
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var cache = core.SchemaServices.GetRequiredService<IPreparedOperationCache>();
                var middleware = new OperationCacheMiddleware(next, diagnosticEvents, cache);
                return context => middleware.InvokeAsync(context);
            },
            nameof(OperationCacheMiddleware));
}

public sealed class OperationInfo : RequestFeature
{
    public string? Id { get; set; }

    public IOperation? Operation { get; set; }

    public OperationDefinitionNode? Definition { get; set; }

    protected internal override void Reset()
    {
        Id = null;
        Definition = null;
        Operation = null;
    }
}

public static class HotChocolateExecutionRequestContextExtensions
{
    public static bool TryGetOperation(
        this RequestContext context,
        [NotNullWhen(true)] out IOperation? operation)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operation = operationInfo.Operation;
        return operation is not null;
    }

    public static bool TryGetOperation(
        this RequestContext context,
        [NotNullWhen(true)] out IOperation? operation,
        [NotNullWhen(true)] out string? operationId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operation = operationInfo.Operation;
        operationId = operationInfo.Id;
        return operation is not null;
    }

    public static IOperation GetOperation(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetRequired<OperationInfo>().Operation
            ?? throw new InvalidOperationException("The operation is not initialized.");
    }

    public static void SetOperation(
        this RequestContext context,
        IOperation operation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operationInfo.Operation = operation;
        operationInfo.Id = operation.Id;
        operationInfo.Definition = operation.Definition;
    }

    public static string? GetOperationId(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetOrSet<OperationInfo>().Id;
    }
}
