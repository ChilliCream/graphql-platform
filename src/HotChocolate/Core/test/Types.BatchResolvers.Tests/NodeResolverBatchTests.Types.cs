using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class NodeResolverBatchTests
{
    private static void Common(IRequestExecutorBuilder builder)
        => builder
            .AddGlobalObjectIdentification()
            .AddNodeIdValueSerializer<CustomKeyNodeIdValueSerializer>()
            .AddObjectType<ClassicEntity>(d =>
                d.ImplementsNode()
                    .IdField(e => e.Id)
                    .ResolveNode((_, id) => Task.FromResult<ClassicEntity?>(new ClassicEntity { Name = id })));

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType<NodeAttributeQuery>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType(NodeQuery.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder
            .AddQueryType(d => d.Name("Query").Field("ready").Resolve(true))
            .AddObjectType<NodeEntity>(d =>
            {
                d.Field(e => e.Name);
                d.ImplementsNode()
                    .IdField(e => e.Id)
                    .ResolveNodeBatchWith<FluentNodeResolvers>(t => t.GetById(default!, default!));
            })
            .AddObjectType<ProtectedNode>(d =>
                d.ImplementsNode()
                    .IdField(e => e.Id)
                    .ResolveNodeBatchWith<FluentNodeResolvers>(t => t.GetProtectedById(default!)))
            .AddObjectType<CustomKeyEntity>(d =>
            {
                d.Field(e => e.Value);
                d.ImplementsNode().IdField(e => e.Id).ResolveNodeBatch(FluentNodeResolvers.GetByCustomKey);
            });
    }
}

/// <summary>
/// A node type resolved through a <c>[NodeResolver][BatchResolver]</c> lookup field, shared by
/// every declaration style.
/// </summary>
public sealed class NodeEntity
{
    public string Id
    {
        get => Name;
        set => Name = value;
    }

    public required string Name { get; set; }
}

/// <summary>
/// A node type resolved through a classic (non-batch) node resolver, fluent-wired identically
/// for every declaration style since the classic path is not the axis under test here; only the
/// mixed batch-and-classic alias row exercises it.
/// </summary>
public sealed class ClassicEntity
{
    public string Id
    {
        get => Name;
        set => Name = value;
    }

    public required string Name { get; set; }
}

/// <summary>
/// A node type whose batch node resolver sits behind a type-level authorization policy
/// (hc-0-bpl.2/.6): node fields skip the regular field middleware, so the batch node resolver
/// pipeline must enforce the policy itself.
/// </summary>
[Authorize("READ_PROTECTED_NODE")]
public sealed class ProtectedNode
{
    public required string Id { get; set; }
}

public sealed class CustomKeyEntity
{
    public required CustomKey Id { get; set; }

    public int Value => Id.Number;
}

public readonly record struct CustomKey(int Number)
{
    public override string ToString() => $"key-{Number}";

    public static CustomKey Parse(string value) => new(int.Parse(value["key-".Length..]));
}

public sealed class CustomKeyNodeIdValueSerializer : INodeIdValueSerializer
{
    public bool IsSupported(Type type) => type == typeof(CustomKey);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is CustomKey key)
        {
            written = System.Text.Encoding.UTF8.GetBytes(key.ToString(), buffer);
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public bool TryParse(
        ReadOnlySpan<byte> buffer,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? value)
    {
        value = CustomKey.Parse(System.Text.Encoding.UTF8.GetString(buffer));
        return true;
    }
}

/// <summary>
/// Fluent-style batch node resolvers bound with <c>ResolveNodeBatchWith</c>/
/// <c>ResolveNodeBatch</c> (hc-0-bpl.6).
/// </summary>
public sealed class FluentNodeResolvers
{
    public List<NodeEntity> GetById(IReadOnlyList<string> id, [Service] BatchProbe probe)
    {
        probe.Record(nameof(GetById), id);
        return id.Select(value => new NodeEntity { Name = value }).ToList();
    }

    public List<ProtectedNode> GetProtectedById(IReadOnlyList<string> id)
        => id.Select(value => new ProtectedNode { Id = value }).ToList();

    public static Task<IReadOnlyList<CustomKeyEntity?>> GetByCustomKey(
        IReadOnlyList<IResolverContext> contexts,
        IReadOnlyList<CustomKey> ids)
        => Task.FromResult<IReadOnlyList<CustomKeyEntity?>>(
            ids.Select(key => (CustomKeyEntity?)new CustomKeyEntity { Id = key }).ToArray());
}

/// <summary>
/// Attribute-style root query exposing every node row through <c>[NodeResolver][BatchResolver]</c>
/// lookup methods.
/// </summary>
public sealed class NodeAttributeQuery
{
    public bool GetReady() => true;

    [NodeResolver]
    [BatchResolver]
    public List<NodeEntity> GetNodeEntityById(List<string> id, BatchProbe probe)
    {
        probe.Record(nameof(GetNodeEntityById), id);
        return id.Select(value => new NodeEntity { Name = value }).ToList();
    }

    [NodeResolver]
    [BatchResolver]
    public List<ProtectedNode> GetProtectedNodeById(List<string> id)
        => id.Select(value => new ProtectedNode { Id = value }).ToList();

    [NodeResolver]
    [BatchResolver]
    public List<CustomKeyEntity> GetCustomKeyEntityById(List<CustomKey> id)
        => id.Select(key => new CustomKeyEntity { Id = key }).ToList();
}

/// <summary>
/// Source-generated root query exposing every node row through
/// <c>[NodeResolver][BatchResolver]</c> lookup methods (hc-0-jyk.2/.6).
/// </summary>
[QueryType]
public static partial class NodeQuery
{
    public static bool GetReady() => true;

    [NodeResolver]
    [BatchResolver]
    public static List<NodeEntity> GetNodeEntityById(List<string> id, BatchProbe probe)
    {
        probe.Record(nameof(GetNodeEntityById), id);
        return id.Select(value => new NodeEntity { Name = value }).ToList();
    }

    [NodeResolver]
    [BatchResolver]
    public static List<ProtectedNode> GetProtectedNodeById(List<string> id)
        => id.Select(value => new ProtectedNode { Id = value }).ToList();

    [NodeResolver]
    [BatchResolver]
    public static List<CustomKeyEntity> GetCustomKeyEntityById(List<CustomKey> id)
        => id.Select(key => new CustomKeyEntity { Id = key }).ToList();
}
