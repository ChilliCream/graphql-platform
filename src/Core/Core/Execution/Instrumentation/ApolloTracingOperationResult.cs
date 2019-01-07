namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingOperationResult
    {
        public long StartOffset { get; set; }

        public long Duration { get; set; }
    }
}
