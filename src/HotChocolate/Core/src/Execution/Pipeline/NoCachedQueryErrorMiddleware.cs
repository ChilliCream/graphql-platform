using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class NoCachedQueryErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IErrorHandler _errorHandler;

        public NoCachedQueryErrorMiddleware(RequestDelegate next, IErrorHandler errorHandler)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public Task InvokeAsync(IRequestContext context)
        {
            if (context.Document is null)
            {
                context.Result = QueryResultBuilder.CreateError(
                    _errorHandler.Handle(ErrorBuilder.New()
                        .SetMessage("CachedQueryNotFound")
                        .SetCode(ErrorCodes.Execution.CachedQueryNotFound)
                        .Build()));
                return Task.CompletedTask;
            }
            else
            {
                return _next.Invoke(context);
            }
        }
    }
}
