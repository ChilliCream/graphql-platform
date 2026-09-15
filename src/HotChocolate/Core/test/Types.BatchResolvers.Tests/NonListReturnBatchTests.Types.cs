using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class NonListReturnBatchTests
{
    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<NonListReturnUser>>>()
                    .Resolve(new List<NonListReturnUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddTypeExtension<NonListReturnUserAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<NonListReturnUser>>>()
                    .Resolve(new List<NonListReturnUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddObjectType<NonListReturnUser>(NonListReturnUserNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<NonListReturnUser>>>()
                    .Resolve(new List<NonListReturnUser> { new(1, "Alice"), new(2, "Bob") });
            })
            .AddObjectType<NonListReturnUser>(d =>
            {
                d.Field(u => u.Name);
                d.Field("greeting")
                    .ResolveBatchWith<FluentNonListReturnResolvers>(t => t.GetGreeting(default!));
            });
}

public sealed record NonListReturnUser(int Id, string Name);

/// <summary>
/// A batch resolver whose return type is a lazy, non-IList sequence. Every declaration style
/// rejects this at schema build (hc-0-6cq.15): distributing a lazy <see cref="IEnumerable{T}"/>
/// positionally would silently drop the exact-count guarantee, so it is a build-time schema
/// error naming the member, identical to the reflection path's message.
/// </summary>
public sealed class FluentNonListReturnResolvers
{
    public Task<IEnumerable<string>> GetGreeting([Parent] List<NonListReturnUser> users)
        => Task.FromResult(users.Select(u => $"Hello, {u.Name}!"));
}

[ExtendObjectType<NonListReturnUser>]
public sealed class NonListReturnUserAttributeExtension
{
    [BatchResolver]
    public Task<IEnumerable<string>> GetGreeting([Parent] List<NonListReturnUser> users)
        => Task.FromResult(users.Select(u => $"Hello, {u.Name}!"));
}

[ObjectType<NonListReturnUser>]
public static partial class NonListReturnUserNode
{
    [BatchResolver]
    public static Task<IEnumerable<string>> GetGreeting([Parent] List<NonListReturnUser> users)
        => Task.FromResult(users.Select(u => $"Hello, {u.Name}!"));
}
