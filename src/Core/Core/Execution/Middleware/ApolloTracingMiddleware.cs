using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution
{
    internal sealed class ApolloTracingMiddleware
    {
        private const string _activityName = "ApolloTracing";
        private const string _extensionKey = "tracing";
        private readonly IApolloTracingResultBuilder _builder;
        private readonly QueryDelegate _next;
        private readonly ITracingOptionsAccessor _options;

        public ApolloTracingMiddleware(
            QueryDelegate next,
            ITracingOptionsAccessor options,
            IApolloTracingResultBuilder builder)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _builder = builder ??
                throw new ArgumentNullException(nameof(builder));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (_options.EnableTracing &&
                context.Result is IQueryResult result)
            {
                Activity activity = new Activity(_activityName).Start();
                long startTimestamp = Timestamp.GetNowInNanoseconds();

                _builder.SetRequestStartTime(
                    activity.StartTimeUtc,
                    startTimestamp);

                try
                {
                    await _next(context).ConfigureAwait(false);
                }
                finally
                {
                    activity.Stop();
                    _builder.SetRequestDuration(activity.Duration);
                    result.Extensions.Add(_extensionKey, _builder.Build());
                }
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }
    }
}
