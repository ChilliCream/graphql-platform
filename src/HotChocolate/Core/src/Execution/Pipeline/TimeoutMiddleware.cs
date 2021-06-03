using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Options;
using static System.Threading.CancellationTokenSource;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class TimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _timeout;

        public TimeoutMiddleware(
            RequestDelegate next,
            IRequestExecutorOptionsAccessor options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _timeout = options.ExecutionTimeout;
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            using var timeout = new CancellationTokenSource(_timeout);
            using var combined = CreateLinkedTokenSource(context.RequestAborted, timeout.Token);

            try
            {
                context.RequestAborted = combined.Token;
                await _next(context).ConfigureAwait(false);

                if (timeout.IsCancellationRequested)
                {
                    context.Result = ErrorHelper.RequestTimeout(_timeout);
                }
            }
            catch (OperationCanceledException)
            {
                // if its not the timeout that canceled we will let somebody else handle this.
                if (!timeout.IsCancellationRequested)
                {
                    throw;
                }

                context.Result = ErrorHelper.RequestTimeout(_timeout);
            }
        }
    }
}

