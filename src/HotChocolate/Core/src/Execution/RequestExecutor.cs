using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutor : IRequestExecutor
    {
        private readonly IServiceProvider _services;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConversion _converter;
        private readonly IActivator _activator;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly RequestDelegate _requestDelegate;

        public RequestExecutor(
            ISchema schema,
            IServiceProvider services,
            IErrorHandler errorHandler,
            ITypeConversion converter,
            IActivator activator,
            IDiagnosticEvents diagnosticEvents, 
            RequestDelegate requestDelegate)
        {
            Schema = schema ?? 
                throw new ArgumentNullException(nameof(schema));
            _services = services ?? 
                throw new ArgumentNullException(nameof(services));
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
        }

        public ISchema Schema { get; }

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

            if(context.Result is null)
            {
                throw new InvalidOperationException();
            }

            return context.Result;
        }
    }
}
