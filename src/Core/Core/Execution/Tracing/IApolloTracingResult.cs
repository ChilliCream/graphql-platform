namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingResult
    {
        int Version { get; }

        string StartTime { get; }

        string EndTime { get; }

        long Duration { get; }

        IApolloTracingRelativeDurationResult Parsing { get; }

        IApolloTracingRelativeDurationResult Validation { get; }

        IApolloTracingExecutionResult Execution { get; }
    }
}
