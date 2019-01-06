namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResult
    {
        public int Version { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public long Duration { get; set; }

        public OperationResult Parsing { get; set; }

        public OperationResult Validation { get; set; }

        public ExecutionResult Execution { get; set; }
    }
}
