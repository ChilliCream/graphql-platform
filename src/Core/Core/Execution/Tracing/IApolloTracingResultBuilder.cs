using System;

namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingResultBuilder
    {
        IApolloTracingResult Build();

        IApolloTracingResultBuilder SetRequestDuration(
            TimeSpan duration);

        IApolloTracingResultBuilder SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp);
    }
}
