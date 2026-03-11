using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Composite;
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

[Node]
public sealed class Issue8057GuidEntity
{
    [ID(nameof(Issue8057GuidEntity))]
    public Guid Id { get; init; }
}

public static class Issue8057GuidEntityOperations
{
    [Lookup]
    [NodeResolver]
    [Query]
    public static Task<Issue8057GuidEntity?> GetIssue8057GuidEntityById(Guid id)
        => Task.FromResult<Issue8057GuidEntity?>(new() { Id = id });
}
