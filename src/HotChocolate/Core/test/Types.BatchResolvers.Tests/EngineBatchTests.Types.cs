using System.Collections.Immutable;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class EngineBatchTests
{
    /// <summary>
    /// The fixed set of users every declaration style resolves nested batch fields against.
    /// </summary>
    public static readonly IReadOnlyList<EngineUser> Users =
    [
        new EngineUser(1, "Alice"),
        new EngineUser(2, "Bob"),
        new EngineUser(3, "Charlie")
    ];

    /// <summary>
    /// The fixed set of products the root <c>productById</c> batch resolver looks up.
    /// </summary>
    public static readonly IReadOnlyList<EngineProduct> Products =
    [
        new EngineProduct(1, "Product 1"),
        new EngineProduct(2, "Product 2")
    ];

    public EngineSerialProbe SerialProbe { get; } = new();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        RegisterServices(builder);
        builder
            .AddQueryType<EngineAttributeQuery>()
            .AddTypeExtension<EngineUserAttributeExtension>()
            .AddMutationType<EngineAttributeMutation>()
            .AddTypeExtension<EnginePayloadItemAttributeExtension>()
            .AddTypeExtension<EngineParentAttributeExtension>()
            .AddTypeExtension<EngineChildAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        RegisterServices(builder);
        builder
            .AddQueryType(EngineQuery.Initialize)
            .AddObjectType<EngineUser>(EngineUserNode.Initialize)
            .AddMutationType(EngineMutation.Initialize)
            .AddObjectType<EnginePayloadItem>(EnginePayloadItemNode.Initialize)
            .AddObjectType<EngineParent>(EngineParentNode.Initialize)
            .AddObjectType<EngineChild>(EngineChildNode.Initialize);
    }

    private void RegisterServices(IRequestExecutorBuilder builder)
        => builder.Services.AddSingleton(SerialProbe).AddSingleton<EngineGreetingService>();

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        RegisterServices(builder);
        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<NonNullType<ListType<NonNullType<ObjectType<EngineUser>>>>>()
                    .Resolve(async ctx =>
                    {
                        ctx.ScopedContextData = ctx.ScopedContextData.SetItem("suffix", "!!!");
                        await Task.Yield();
                        return Users.ToList();
                    });
                d.Field("productById")
                    .Argument("id", a => a.Type<IntType>())
                    .ResolveBatchWith<FluentEngineQueryResolvers>(t => t.GetProductById(null!, null!));
                d.Field("asyncUsers")
                    .Type<NonNullType<ListType<NonNullType<ObjectType<EngineUser>>>>>()
                    .Resolve(async _ =>
                    {
                        await Task.Delay(1);
                        return Users.ToList();
                    });
            })
            .AddObjectType<EngineUser>(d =>
            {
                d.Field("greeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetGreeting(default!, default!));
                d.Field("greetingWithArgument")
                    .Argument("prefix", a => a.Type<StringType>())
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetGreetingWithArgument(default!, default!));
                d.Field("greetingWithService")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetGreetingWithService(default!, default!));
                d.Field("greetingWithGlobalState")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetGreetingWithGlobalState(default!, default!));
                d.Field("greetingWithScopedState")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetGreetingWithScopedState(default!, default!));
                d.Field("greetingWithCancellationToken")
                    .ResolveBatchWith<FluentEngineUserResolvers>(
                        t => t.GetGreetingWithCancellationToken(default!, default));
                d.Field("asyncGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetAsyncGreeting(default!));
                d.Field("asyncValueGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetAsyncValueGreeting(default!));
                d.Field("mismatchGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetMismatchGreeting(default!));
                d.Field("readOnlyGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetReadOnlyGreeting(default!));
                d.Field("arrayGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetArrayGreeting(default!));
                d.Field("immutableGreeting")
                    .ResolveBatchWith<FluentEngineUserResolvers>(t => t.GetImmutableGreeting(default!));
            })
            .AddMutationType(d =>
            {
                d.Name("Mutation");
                d.Field("doIt").Resolve(_ => new EngineDoItPayload(
                [
                    new EnginePayloadItem(1),
                    new EnginePayloadItem(2),
                    new EnginePayloadItem(3)
                ]));
                foreach (var name in new[] { "createParent", "cloneParent" })
                {
                    d.Field(name)
                        .Argument("id", a => a.Type<NonNullType<IntType>>())
                        .Type<ObjectType<EngineParent>>()
                        .Resolve(async ctx =>
                        {
                            var id = ctx.ArgumentValue<int>("id");
                            var probe = ctx.Service<EngineSerialProbe>();
                            probe.Events.Add($"mutation-{id}-start");
                            await Task.Delay(10, ctx.RequestAborted);
                            return new EngineParent(id);
                        });
                }
            })
            .AddObjectType<EnginePayloadItem>(d =>
                d.Field("stock").ResolveBatchWith<FluentEnginePayloadResolvers>(t => t.GetStock(default!, default!)))
            .AddObjectType<EngineParent>(d =>
            {
                d.Field(p => p.Id);
                d.Field("children")
                    .Type<NonNullType<ListType<NonNullType<ObjectType<EngineChild>>>>>()
                    .Resolve(async ctx =>
                {
                    await Task.Delay(25, ctx.RequestAborted);
                    return EngineChild.CreateFor(ctx.Parent<EngineParent>().Id);
                });
            })
            .AddObjectType<EngineChild>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed")
                    .Type<StringType>()
                    .ResolveBatchWith<FluentEngineChildResolvers>(t => t.GetComputed(default!, default!));
            });
    }
}

/// <summary>
/// A user whose nested fields are resolved through per-shape batch resolvers.
/// </summary>
public sealed record EngineUser(int Id, string Name);

/// <summary>
/// A product resolved through a root-level batch resolver, used to prove alias coalescing.
/// </summary>
public sealed record EngineProduct(int Id, string Name);

/// <summary>
/// A mutation payload item whose <c>stock</c> field coalesces below the serial mutation root.
/// </summary>
public sealed record EnginePayloadItem(int Id);

public sealed record EngineDoItPayload(List<EnginePayloadItem> Items);

/// <summary>
/// The parent of two serial-mutation-created subtrees whose async <c>children</c> feed a batch
/// field; proves batches never span two serial mutation steps (hc-0-6cq.18).
/// </summary>
public sealed record EngineParent(int Id);

/// <summary>
/// A child resolved from <see cref="EngineParent.Id"/>; <c>computed</c> is a batch field that
/// records both its batch size and completion order into <see cref="EngineSerialProbe"/>.
/// </summary>
public sealed record EngineChild(int Id)
{
    public static List<EngineChild> CreateFor(int parentId) => [new(parentId * 10 + 1), new(parentId * 10 + 2)];
}

/// <summary>
/// A per-execution event log for the serial-mutation proof: batch sizes observed by the
/// <c>computed</c> batch field, and an ordered log of mutation starts and batch completions.
/// </summary>
public sealed class EngineSerialProbe
{
    public List<int> BatchSizes { get; } = [];

    public List<string> Events { get; } = [];
}

public sealed class EngineGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}

// -- Fluent -----------------------------------------------------------------------------------

/// <summary>
/// Fluent-style root batch resolver bound with <c>ResolveBatchWith</c>.
/// </summary>
public sealed class FluentEngineQueryResolvers
{
    public List<EngineProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => EngineBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Fluent-style batch resolvers for every parent shape and injection kind bound with
/// <c>ResolveBatchWith</c>.
/// </summary>
public sealed class FluentEngineUserResolvers
{
    public List<string> GetGreeting([Parent] List<EngineUser> users, BatchProbe probe)
    {
        probe.Record(nameof(GetGreeting), users.Select(u => u.Id));
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    public List<string> GetGreetingWithArgument([Parent] List<EngineUser> users, List<string> prefix)
        => users.Zip(prefix, (u, p) => $"{p}, {u.Name}!").ToList();

    public List<string> GetGreetingWithService(
        [Parent] List<EngineUser> users,
        [Service] EngineGreetingService service)
        => users.ConvertAll(u => service.Greet(u.Name));

    public List<string> GetGreetingWithGlobalState(
        [Parent] List<EngineUser> users,
        [GlobalState("prefix")] string prefix)
        => users.ConvertAll(u => $"{prefix}, {u.Name}!");

    public List<string> GetGreetingWithScopedState(
        [Parent] List<EngineUser> users,
        [ScopedState("suffix")] string suffix)
        => users.ConvertAll(u => $"Hello, {u.Name}{suffix}");

    public List<string> GetGreetingWithCancellationToken(
        [Parent] List<EngineUser> users,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    public async Task<List<string>> GetAsyncGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    public async ValueTask<List<string>> GetAsyncValueGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    /// <summary>
    /// Deliberately returns one fewer result than contexts, to prove that every context fails
    /// when the batch resolver's result count does not match.
    /// </summary>
    public List<string> GetMismatchGreeting([Parent] List<EngineUser> users)
        => users.Take(users.Count - 1).Select(u => $"Hello, {u.Name}!").ToList();

    public List<string> GetReadOnlyGreeting([Parent] IReadOnlyList<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();

    public string[] GetArrayGreeting([Parent] EngineUser[] users)
        => users.Select(u => $"Hello, {u.Name}!").ToArray();

    public List<string> GetImmutableGreeting([Parent] ImmutableArray<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();
}

/// <summary>
/// Fluent-style batch resolver for a field declared inside a mutation payload.
/// </summary>
public sealed class FluentEnginePayloadResolvers
{
    public List<int> GetStock([Parent] List<EnginePayloadItem> items, BatchProbe probe)
    {
        probe.Record(nameof(GetStock), items.Select(i => i.Id));
        return items.ConvertAll(i => i.Id * 100);
    }
}

/// <summary>
/// Fluent-style batch resolver that records batch size and completion order for the serial
/// mutation proof.
/// </summary>
public sealed class FluentEngineChildResolvers
{
    public List<string> GetComputed([Parent] List<EngineChild> children, EngineSerialProbe probe)
        => EngineChildLogic.Compute(children, probe);
}

internal static class EngineChildLogic
{
    public static List<string> Compute(IReadOnlyList<EngineChild> children, EngineSerialProbe probe)
    {
        probe.BatchSizes.Add(children.Count);
        probe.Events.Add($"batch-{children[0].Id / 10}-complete");
        return children.Select(c => $"c{c.Id}").ToList();
    }
}

// -- Attribute ----------------------------------------------------------------------------------

/// <summary>
/// Attribute-style root query, including a batch resolver that resolves products by id.
/// </summary>
public sealed class EngineAttributeQuery
{
    public async Task<List<EngineUser>> GetUsers(IResolverContext context)
    {
        context.ScopedContextData = context.ScopedContextData.SetItem("suffix", "!!!");
        await Task.Yield();
        return EngineBatchTests.Users.ToList();
    }

    [BatchResolver]
    public List<EngineProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => EngineBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }

    /// <summary>
    /// A parent resolver dedicated to the #9892 regression: it is async and unshared with any
    /// other row, so this family alone proves an async parent feeding a nested batch field.
    /// </summary>
    public async Task<List<EngineUser>> GetAsyncUsers()
    {
        await Task.Delay(1);
        return EngineBatchTests.Users.ToList();
    }
}

/// <summary>
/// Attribute-style batch resolvers for every parent shape and injection kind.
/// </summary>
[ExtendObjectType<EngineUser>]
public sealed class EngineUserAttributeExtension
{
    [BatchResolver]
    public List<string> GetGreeting([Parent] List<EngineUser> users, BatchProbe probe)
    {
        probe.Record(nameof(GetGreeting), users.Select(u => u.Id));
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public List<string> GetGreetingWithArgument([Parent] List<EngineUser> users, List<string> prefix)
        => users.Zip(prefix, (u, p) => $"{p}, {u.Name}!").ToList();

    [BatchResolver]
    public List<string> GetGreetingWithService(
        [Parent] List<EngineUser> users,
        [Service] EngineGreetingService service)
        => users.ConvertAll(u => service.Greet(u.Name));

    [BatchResolver]
    public List<string> GetGreetingWithGlobalState(
        [Parent] List<EngineUser> users,
        [GlobalState("prefix")] string prefix)
        => users.ConvertAll(u => $"{prefix}, {u.Name}!");

    [BatchResolver]
    public List<string> GetGreetingWithScopedState(
        [Parent] List<EngineUser> users,
        [ScopedState("suffix")] string suffix)
        => users.ConvertAll(u => $"Hello, {u.Name}{suffix}");

    [BatchResolver]
    public List<string> GetGreetingWithCancellationToken(
        [Parent] List<EngineUser> users,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public async Task<List<string>> GetAsyncGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public async ValueTask<List<string>> GetAsyncValueGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public List<string> GetMismatchGreeting([Parent] List<EngineUser> users)
        => users.Take(users.Count - 1).Select(u => $"Hello, {u.Name}!").ToList();

    [BatchResolver]
    public List<string> GetReadOnlyGreeting([Parent] IReadOnlyList<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();

    [BatchResolver]
    public string[] GetArrayGreeting([Parent] EngineUser[] users)
        => users.Select(u => $"Hello, {u.Name}!").ToArray();

    [BatchResolver]
    public List<string> GetImmutableGreeting([Parent] ImmutableArray<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();
}

public sealed class EngineAttributeMutation
{
    public EngineDoItPayload DoIt() => new(
    [
        new EnginePayloadItem(1),
        new EnginePayloadItem(2),
        new EnginePayloadItem(3)
    ]);

    public async Task<EngineParent> CreateParent(int id, [Service] EngineSerialProbe probe, CancellationToken ct)
    {
        probe.Events.Add($"mutation-{id}-start");
        await Task.Delay(10, ct);
        return new EngineParent(id);
    }

    public async Task<EngineParent> CloneParent(int id, [Service] EngineSerialProbe probe, CancellationToken ct)
    {
        probe.Events.Add($"mutation-{id}-start");
        await Task.Delay(10, ct);
        return new EngineParent(id);
    }
}

[ExtendObjectType<EnginePayloadItem>]
public sealed class EnginePayloadItemAttributeExtension
{
    [BatchResolver]
    public List<int> GetStock([Parent] List<EnginePayloadItem> items, BatchProbe probe)
    {
        probe.Record(nameof(GetStock), items.Select(i => i.Id));
        return items.ConvertAll(i => i.Id * 100);
    }
}

[ExtendObjectType<EngineParent>]
public sealed class EngineParentAttributeExtension
{
    public async Task<List<EngineChild>> GetChildren([Parent] EngineParent parent, CancellationToken ct)
    {
        await Task.Delay(25, ct);
        return EngineChild.CreateFor(parent.Id);
    }
}

[ExtendObjectType<EngineChild>]
public sealed class EngineChildAttributeExtension
{
    [BatchResolver]
    public List<string> GetComputed([Parent] List<EngineChild> children, [Service] EngineSerialProbe probe)
        => EngineChildLogic.Compute(children, probe);
}

// -- Source generated -----------------------------------------------------------------------

/// <summary>
/// Source-generated root query, including a batch resolver that resolves products by id.
/// </summary>
[QueryType]
public static partial class EngineQuery
{
    public static async Task<List<EngineUser>> GetUsers(IResolverContext context)
    {
        context.ScopedContextData = context.ScopedContextData.SetItem("suffix", "!!!");
        await Task.Yield();
        return EngineBatchTests.Users.ToList();
    }

    [BatchResolver]
    public static List<EngineProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => EngineBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }

    /// <summary>
    /// A parent resolver dedicated to the #9892 regression: it is async and unshared with any
    /// other row, so this family alone proves an async parent feeding a nested batch field.
    /// </summary>
    public static async Task<List<EngineUser>> GetAsyncUsers()
    {
        await Task.Delay(1);
        return EngineBatchTests.Users.ToList();
    }
}

/// <summary>
/// Source-generated batch resolvers for every parent shape and injection kind.
/// </summary>
[ObjectType<EngineUser>]
public static partial class EngineUserNode
{
    [BatchResolver]
    public static List<string> GetGreeting([Parent] List<EngineUser> users, BatchProbe probe)
    {
        probe.Record(nameof(GetGreeting), users.Select(u => u.Id));
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public static List<string> GetGreetingWithArgument([Parent] List<EngineUser> users, List<string> prefix)
        => users.Zip(prefix, (u, p) => $"{p}, {u.Name}!").ToList();

    [BatchResolver]
    public static List<string> GetGreetingWithService(
        [Parent] List<EngineUser> users,
        [Service] EngineGreetingService service)
        => users.ConvertAll(u => service.Greet(u.Name));

    [BatchResolver]
    public static List<string> GetGreetingWithGlobalState(
        [Parent] List<EngineUser> users,
        [GlobalState("prefix")] string prefix)
        => users.ConvertAll(u => $"{prefix}, {u.Name}!");

    [BatchResolver]
    public static List<string> GetGreetingWithScopedState(
        [Parent] List<EngineUser> users,
        [ScopedState("suffix")] string suffix)
        => users.ConvertAll(u => $"Hello, {u.Name}{suffix}");

    [BatchResolver]
    public static List<string> GetGreetingWithCancellationToken(
        [Parent] List<EngineUser> users,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public static async Task<List<string>> GetAsyncGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public static async ValueTask<List<string>> GetAsyncValueGreeting([Parent] List<EngineUser> users)
    {
        await Task.Yield();
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public static List<string> GetMismatchGreeting([Parent] List<EngineUser> users)
        => users.Take(users.Count - 1).Select(u => $"Hello, {u.Name}!").ToList();

    [BatchResolver]
    public static List<string> GetReadOnlyGreeting([Parent] IReadOnlyList<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();

    [BatchResolver]
    public static string[] GetArrayGreeting([Parent] EngineUser[] users)
        => users.Select(u => $"Hello, {u.Name}!").ToArray();

    [BatchResolver]
    public static List<string> GetImmutableGreeting([Parent] ImmutableArray<EngineUser> users)
        => users.Select(u => $"Hello, {u.Name}!").ToList();
}

[MutationType]
public static partial class EngineMutation
{
    public static EngineDoItPayload DoIt() => new(
    [
        new EnginePayloadItem(1),
        new EnginePayloadItem(2),
        new EnginePayloadItem(3)
    ]);

    public static async Task<EngineParent> CreateParent(int id, [Service] EngineSerialProbe probe, CancellationToken ct)
    {
        probe.Events.Add($"mutation-{id}-start");
        await Task.Delay(10, ct);
        return new EngineParent(id);
    }

    public static async Task<EngineParent> CloneParent(int id, [Service] EngineSerialProbe probe, CancellationToken ct)
    {
        probe.Events.Add($"mutation-{id}-start");
        await Task.Delay(10, ct);
        return new EngineParent(id);
    }
}

[ObjectType<EnginePayloadItem>]
public static partial class EnginePayloadItemNode
{
    [BatchResolver]
    public static List<int> GetStock([Parent] List<EnginePayloadItem> items, BatchProbe probe)
    {
        probe.Record(nameof(GetStock), items.Select(i => i.Id));
        return items.ConvertAll(i => i.Id * 100);
    }
}

[ObjectType<EngineParent>]
public static partial class EngineParentNode
{
    public static async Task<List<EngineChild>> GetChildren([Parent] EngineParent parent, CancellationToken ct)
    {
        await Task.Delay(25, ct);
        return EngineChild.CreateFor(parent.Id);
    }
}

[ObjectType<EngineChild>]
public static partial class EngineChildNode
{
    [BatchResolver]
    public static List<string> GetComputed([Parent] List<EngineChild> children, [Service] EngineSerialProbe probe)
        => EngineChildLogic.Compute(children, probe);
}
