using System;

namespace HotChocolate.Execution.Tracing
{
    internal class ApolloTracingResultBuilder
        : IApolloTracingResultBuilder
    {
        public IApolloTracingResult Build()
        {
            throw new NotImplementedException();
        }

        public IApolloTracingResultBuilder SetRequestDuration(
            TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public IApolloTracingResultBuilder SetRequestStartTime(
            DateTimeOffset startTime,
            long startTimestamp)
        {
            throw new NotImplementedException();
        }
    }
}
