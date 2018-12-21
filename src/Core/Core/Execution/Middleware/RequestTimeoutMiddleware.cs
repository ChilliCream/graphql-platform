using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Execution
{
    internal sealed class RequestTimeoutMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IRequestTimeoutOptionsAccessor _options;

        public RequestTimeoutMiddleware(
            QueryDelegate next,
            IRequestTimeoutOptionsAccessor options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            var requestTimeoutCts = new CancellationTokenSource(
                _options.ExecutionTimeout);

            try
            {
                using (var combinedCts = CancellationTokenSource
                    .CreateLinkedTokenSource(requestTimeoutCts.Token,
                        context.RequestAborted))
                {
                    context.RequestAborted = combinedCts.Token;
                    await _next(context);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (requestTimeoutCts.IsCancellationRequested)
                {
                    context.Exception = ex;
                    context.Result = new QueryResult(
                        new QueryError("Execution timeout has been exceeded."));
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                requestTimeoutCts.Dispose();
            }
        }
    }
}
