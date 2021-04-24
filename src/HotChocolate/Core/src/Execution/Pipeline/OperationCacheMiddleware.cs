using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IPreparedOperationCache _operationCache;

        public OperationCacheMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IPreparedOperationCache operationCache)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _operationCache = operationCache ??
                throw new ArgumentNullException(nameof(operationCache));
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            if (context.DocumentId is null)
            {
                await _next(context).ConfigureAwait(false);
            }
            else
            {
                var addToCache = true;
                var operationId = context.OperationId;

                if (operationId is null)
                {
                    operationId = CreateOperationId(
                        context.DocumentId,
                        context.Request.OperationName);
                    context.OperationId = operationId;
                }

                string cacheId = $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";

                if (_operationCache.TryGetOperation(cacheId, out IPreparedOperation? operation))
                {
                    context.Operation = operation;
                    addToCache = false;
                    _diagnosticEvents.RetrievedOperationFromCache(context);
                }

                await _next(context).ConfigureAwait(false);

                if (addToCache &&
                    context.Operation is { } &&
                    context.DocumentId is { } &&
                    context.Document is { } &&
                    context.ValidationResult is { HasErrors: false })
                {
                    _operationCache.TryAddOperation(cacheId, context.Operation);
                    _diagnosticEvents.AddedOperationToCache(context);
                }
            }
        }
    }
}
