using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal sealed class RequestTimeoutMiddleware
    {
        private readonly QueryDelegate _next;

        public RequestTimeoutMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            using (var requestTimeoutCts =
                new CancellationTokenSource(
                    context.Schema.Options.ExecutionTimeout))
            {

                using (var combinedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        requestTimeoutCts.Token, context.RequestAborted))
                {
                    context.RequestAborted = combinedCts.Token;
                    await _next(context);
                }
            }
        }
    }
}
