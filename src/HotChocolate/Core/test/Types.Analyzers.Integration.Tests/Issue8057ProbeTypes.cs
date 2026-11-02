using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[Node]
public sealed class Issue8057Entity
{
    [ID(nameof(Issue8057Entity))]
    public Issue8057EntityId Id { get; init; }
}

public readonly struct Issue8057EntityId;

public static class Issue8057EntityOperations
{
    [NodeResolver]
    [Query]
    public static Issue8057Entity? GetIssue8057Entity(
        Issue8057EntityId id,
        IResolverContext resolverContext)
        => new() { Id = id };
}
