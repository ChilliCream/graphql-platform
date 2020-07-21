using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Batching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutor : IRequestExecutor
    {
        private readonly IServiceProvider _services;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConverter _converter;
        private readonly IActivator _activator;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly RequestDelegate _requestDelegate;
        private readonly BatchExecutor _batchExecutor;

        public RequestExecutor(
            ISchema schema,
            IServiceProvider applicationServices,
            IServiceProvider schemaServices,
            IErrorHandler errorHandler,
            ITypeConverter converter,
            IActivator activator,
            IDiagnosticEvents diagnosticEvents,
            RequestDelegate requestDelegate)
        {
            Schema = schema ??
                throw new ArgumentNullException(nameof(schema));
            _services = applicationServices ??
                throw new ArgumentNullException(nameof(applicationServices));
            Services = schemaServices ?? 
                throw new ArgumentNullException(nameof(schemaServices));
            _errorHandler = errorHandler ??
                throw new ArgumentNullException(nameof(errorHandler));
            _converter = converter ??
                throw new ArgumentNullException(nameof(converter));
            _activator = activator ??
                throw new ArgumentNullException(nameof(activator));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _requestDelegate = requestDelegate ??
                throw new ArgumentNullException(nameof(requestDelegate));
            _batchExecutor = new BatchExecutor(this, errorHandler, converter);
        }

        public ISchema Schema { get; }

        public IServiceProvider Services { get; }

        public async Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var context = new RequestContext(
                Schema,
                request.Services ?? _services,
                _errorHandler,
                _converter,
                _activator,
                _diagnosticEvents,
                request)
            {
                RequestAborted = cancellationToken
            };

            await _requestDelegate(context).ConfigureAwait(false);

            if (context.Result is null)
            {
                throw new InvalidOperationException();
            }

            return context.Result;
        }

        public Task<IBatchQueryResult> ExecuteBatchAsync(
            IEnumerable<IReadOnlyQueryRequest> requestBatch,
            bool allowParallelExecution = false,
            CancellationToken cancellationToken = default)
        {
            if (requestBatch is null)
            {
                throw new ArgumentNullException(nameof(requestBatch));
            }

            return Task.FromResult<IBatchQueryResult>(new BatchQueryResult(
                () => _batchExecutor.ExecuteAsync(requestBatch, cancellationToken),
                null));
        }
    }
}
