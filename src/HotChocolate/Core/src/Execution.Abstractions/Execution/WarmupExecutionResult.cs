namespace HotChocolate.Execution;

public sealed class WarmupExecutionResult : ExecutionResult
{
    public override ExecutionResultKind Kind => ExecutionResultKind.WarmupResult;

    public override IReadOnlyDictionary<string, object?>? ContextData => null;
}
