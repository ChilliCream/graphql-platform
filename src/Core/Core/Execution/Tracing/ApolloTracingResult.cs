namespace HotChocolate.Execution.Tracing
{
    internal sealed class ApolloTracingResult
    {
        public int Version { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public long Duration { get; set; }

        public ApolloTracingParsingResult Parsing { get; set; }

        public ApolloTracingValidationResult Validation { get; set; }

        public ApolloTracingExecutionResult Execution { get; set; }
    }
}
