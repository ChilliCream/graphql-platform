using System;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DiagnosticAdapter;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingDiagnosticListener
        : DiagnosticListener
    {
        private const string _extensionKey = "tracing";
        private readonly IApolloTracingResultBuilder _builder;

        public ApolloTracingDiagnosticListener(
            IApolloTracingResultBuilder builder)
                : base(Constants.DiagnosticListenerName)
        {
            _builder = builder ??
                throw new ArgumentNullException(nameof(builder));
        }

        [DiagnosticName(Constants.QueryActivityName + ".Start")]
        public void BeginQueryExecute()
        {
            _builder.SetRequestStartTime(
                Activity.Current.StartTimeUtc,
                Timestamp.GetNowInNanoseconds());
        }

        [DiagnosticName(Constants.QueryActivityName + ".Stop")]
        public void BeginQueryExecute(IExecutionResult executionResult)
        {
            _builder.SetRequestDuration(Activity.Current.Duration);

            if (executionResult is IQueryResult result)
            {
                result.Extensions.Add(_extensionKey, _builder.Build());
            }
        }

        [DiagnosticName(Constants.ResolverActivityName + ".Start")]
        public void BeginResolveField()
        {
            Activity.Current.AddTag(
                "startTimestamp",
                Timestamp.GetNowInNanoseconds().ToString());
        }

        [DiagnosticName(Constants.ResolverActivityName + ".Stop")]
        public void EndResolveField(IResolverContext context)
        {
            long stopTimestamp = Timestamp.GetNowInNanoseconds();
            long startTimestamp = Convert.ToInt64(Activity.Current.Tags
                .First(t => t.Key == "startTimestamp"));

            _builder.AddResolverResult(new ApolloTracingResolverStatistics(
                context)
            {
                StartTimestamp = startTimestamp,
                EndTimestamp = stopTimestamp
            });
        }
    }
}
