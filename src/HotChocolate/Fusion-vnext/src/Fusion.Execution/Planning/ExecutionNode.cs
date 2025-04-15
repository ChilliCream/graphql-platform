namespace HotChocolate.Fusion.Planning;

public abstract record ExecutionNode
{
    public required int Id { get; init; }
}
