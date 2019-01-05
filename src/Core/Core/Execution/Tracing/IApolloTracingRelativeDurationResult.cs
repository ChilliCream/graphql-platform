namespace HotChocolate.Execution.Tracing
{
    internal interface IApolloTracingRelativeDurationResult
    {
        long StartOffset { get; }

        long Duration { get; }
    }
}
