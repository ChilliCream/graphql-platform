using System;

namespace HotChocolate.Execution.Instrumentation
{
    internal interface IApolloTracingResultBuilder
    {
        void SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp);

        void SetParsingResult(long startTimestamp, long endTimestamp);

        void SetValidationResult(long startTimestamp, long endTimestamp);

        void AddResolverResult(
            ApolloTracingResolverStatistics resolverStatistics);

        void SetRequestDuration(TimeSpan duration);

        ApolloTracingResult Build();
    }
}
