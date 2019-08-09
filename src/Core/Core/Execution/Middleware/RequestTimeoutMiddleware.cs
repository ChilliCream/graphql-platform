using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;

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
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            CancellationTokenSource requestTimeoutCts = null;
            CancellationTokenSource combinedCts = null;
            CancellationToken requestAborted = context.RequestAborted;

            if (!Debugger.IsAttached)
            {
                requestTimeoutCts = new CancellationTokenSource(
                    _options.ExecutionTimeout);
                combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                   requestTimeoutCts.Token,
                   context.RequestAborted);
                context.RequestAborted = combinedCts.Token;
            }

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                if (requestAborted.IsCancellationRequested)
                {
                    throw;
                }

                context.Exception = ex;
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.RequestTimeoutMiddleware_Timeout)
                        .SetCode(MiddlewareErrorCodes.Timeout)
                        .Build());
            }
            finally
            {
                combinedCts?.Dispose();
                requestTimeoutCts?.Dispose();
            }
        }
    }
}
