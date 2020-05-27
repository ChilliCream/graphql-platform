using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly RequestDelegate _requestDelegate;

        public RequestExecutor(
            ISchema schema,
            IServiceProvider services,
            IErrorHandler errorHandler,
            ITypeConversion converter,
            IActivator activator,
            RequestDelegate requestDelegate)
        {
            Schema = schema;
            _services = services;
            _errorHandler = errorHandler;
            _converter = converter;
            _activator = activator;
            _requestDelegate = requestDelegate;
        }

        public ISchema Schema { get; }

        public async Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            var context = new RequestContext(
                Schema,
                request.Services ?? _services,
                _errorHandler,
                _converter,
                _activator,
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
