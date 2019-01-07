namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResult
    {
        public int Version { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public long Duration { get; set; }

        public ApolloTracingOperationResult Parsing { get; set; }

        public ApolloTracingOperationResult Validation { get; set; }

        public ApolloTracingExecutionResult Execution { get; set; }
    }
}
