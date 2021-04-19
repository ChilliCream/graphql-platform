using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Batching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutor : IRequestExecutor
    {
        private readonly DefaultRequestContextAccessor _requestContextAccessor;
        private readonly IServiceProvider _applicationServices;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConverter _converter;
        private readonly IActivator _activator;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly RequestDelegate _requestDelegate;
        private readonly BatchExecutor _batchExecutor;

        public RequestExecutor(
            ISchema schema,
            DefaultRequestContextAccessor requestContextAccessor,
            IServiceProvider applicationServices,
            IServiceProvider executorServices,
            IErrorHandler errorHandler,
            ITypeConverter converter,
            IActivator activator,
            IDiagnosticEvents diagnosticEvents,
            RequestDelegate requestDelegate,
            ulong version)
        {
            Schema = schema ??
                throw new ArgumentNullException(nameof(schema));
            _requestContextAccessor = requestContextAccessor ??
                throw new ArgumentNullException(nameof(requestContextAccessor));
            _applicationServices = applicationServices ??
                throw new ArgumentNullException(nameof(applicationServices));
            Services = executorServices ??
                throw new ArgumentNullException(nameof(executorServices));
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
            Version = version;
            _batchExecutor = new BatchExecutor(this, errorHandler, converter);
        }

        public ISchema Schema { get; }

        public IServiceProvider Services { get; }

        public ulong Version { get; }

        public async Task<IExecutionResult> ExecuteAsync(
            IQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IServiceScope? scope = request.Services is null ? _applicationServices.CreateScope() : null;
            IServiceProvider services = scope is null ? request.Services! : scope.ServiceProvider;

            try
            {
                var context = new RequestContext(
                    Schema,
                    Version,
                    services,
                    _errorHandler,
                    _converter,
                    _activator,
                    _diagnosticEvents,
                    request)
                {
                    RequestAborted = cancellationToken
                };

                _requestContextAccessor.RequestContext = context;

                // ensure that all subtasks spawned from this are started in the correct scheduler
                // (this check can be removed once the experimental batching mode becomes the only option
                TaskScheduler scheduler = services.GetRequiredService<IContextBatchDispatcher>().TaskScheduler;
                if (scheduler == TaskScheduler.Current)
                {
                    await _requestDelegate(context).ConfigureAwait(false);
                }
                else
                {
                    await Task.Factory.StartNew(
                        () => _requestDelegate(context).AsTask(),
                        cancellationToken,
                        TaskCreationOptions.None,
                        scheduler).Unwrap().ConfigureAwait(false);
                }

                if (context.Result is null)
                {
                    throw new InvalidOperationException();
                }

                if (scope is not null)
                {
                    if (context.Result is DeferredQueryResult deferred)
                    {
                        context.Result = new DeferredQueryResult(deferred, scope);
                        scope = null;
                    }
                    else if (context.Result is SubscriptionResult result)
                    {
                        context.Result = new SubscriptionResult(result, scope);
                        scope = null;
                    }
                }

                return context.Result;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public Task<IBatchQueryResult> ExecuteBatchAsync(
            IEnumerable<IQueryRequest> requestBatch,
            bool allowParallelExecution = false,
            CancellationToken cancellationToken = default)
        {
            if (requestBatch is null)
            {
                throw new ArgumentNullException(nameof(requestBatch));
            }

            // ensure that all subtasks spawned from this are started in the correct scheduler
            // (this check can be removed once the experimental batching mode becomes the only option
            var scheduler = GetScheduler(requestBatch);
            if (scheduler == null || scheduler == TaskScheduler.Current)
            {
                return Task.FromResult<IBatchQueryResult>(new BatchQueryResult(
                    () => _batchExecutor.ExecuteAsync(requestBatch, allowParallelExecution, cancellationToken), null));
            }
            else
            {
                return Task.Factory.StartNew<IBatchQueryResult>(
                    () => new BatchQueryResult(
                        () => _batchExecutor.ExecuteAsync(requestBatch, allowParallelExecution, cancellationToken),
                        null),
                    cancellationToken,
                    TaskCreationOptions.None,
                    scheduler);
            }
        }

        private TaskScheduler? GetScheduler(IEnumerable<IQueryRequest> requestBatch)
        {
            var taskSchedulers = requestBatch.Select(
                x => x.Services?.GetRequiredService<IContextBatchDispatcher>().TaskScheduler
                    ?? _applicationServices.GetRequiredService<IContextBatchDispatcher>().TaskScheduler)
                .Distinct().ToList();
            if (taskSchedulers.Count == 1)
            {
                return taskSchedulers[0];
            }
            else
            {
                Debug.Assert(taskSchedulers.Count() == 0, "Not all requests in the batch use the same scheduler");
                return null;
            }
        }
    }
}
