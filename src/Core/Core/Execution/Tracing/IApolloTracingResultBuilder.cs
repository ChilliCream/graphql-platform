using System;

namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingResultBuilder
    {
        IApolloTracingResultBuilder SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp);

        IApolloTracingResultBuilder SetParsingResult(
            long startOffset,
            long duration);

        IApolloTracingResultBuilder SetValidationResult(
            long startOffset,
            long duration);

        IApolloTracingResultBuilder AddResolverResult(
            ApolloTracingResolverResult result);

        IApolloTracingResultBuilder SetRequestDuration(
            TimeSpan duration);

        ApolloTracingResult Build();
    }
}
