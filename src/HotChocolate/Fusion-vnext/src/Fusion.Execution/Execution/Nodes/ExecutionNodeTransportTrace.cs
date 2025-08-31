namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record ExecutionNodeTransportTrace
{
    public required Uri Uri { get; init; }

    public required string ContentType { get; init; }
}
