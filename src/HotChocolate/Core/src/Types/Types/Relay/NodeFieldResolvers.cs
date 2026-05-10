using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This helper class contains the resolvers for the node and nodes fields.
/// </summary>
internal static class NodeFieldResolvers
{
    /// <summary>
    /// This is the batch resolver of the node field.
    /// </summary>
    public static async ValueTask ResolveNodeBatchAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        INodeIdSerializerAccessor serializerAccessor)
    {
        if (contexts.Length == 0)
        {
            return;
        }

        var serializer = serializerAccessor.Serializer;
        var first = contexts[0];
        var schema = first.Schema;
        var deserializedId = ResolveNodeId(first, serializer, Id);
        var typeName = deserializedId.TypeName;

        if (!schema.Types.TryGetType<ObjectType>(typeName, out var type)
            || type.Features.Get<NodeTypeFeature>() is not { NodeResolver: { } nodeResolver })
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                var ctx = contexts[i];
                ctx.ReportError(ErrorHelper.Relay_NoNodeResolver(typeName, ctx.Path));
                ctx.Result = null;
            }
            return;
        }

        var typeConverter = first.Service<ITypeConverter>();

        for (var i = 0; i < contexts.Length; i++)
        {
            var ctx = contexts[i];
            var nodeId = ctx.ArgumentLiteral<StringValueNode>(Id);
            var localId = i == 0 ? deserializedId : ResolveNodeId(ctx, serializer, Id);
            SetLocalContext(ctx, nodeId, localId, type);
            TryReplaceArguments(ctx, nodeResolver, Id, nodeId);
        }

        await DispatchAsync(contexts, nodeResolver).ConfigureAwait(false);

        for (var i = 0; i < contexts.Length; i++)
        {
            var ctx = contexts[i];
            ctx.Result = CoerceResult(ctx.Result, type, typeConverter);
        }
    }

    /// <summary>
    /// This is the batch resolver of the nodes field.
    /// </summary>
    public static async ValueTask ResolveNodesBatchAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        INodeIdSerializerAccessor serializerAccessor,
        int maxAllowedNodes)
    {
        if (contexts.Length == 0)
        {
            return;
        }

        var serializer = serializerAccessor.Serializer;
        var schema = contexts[0].Schema;
        var typeConverter = contexts[0].Service<ITypeConverter>();

        // Per parent context: parse all IDs, allocate the result array, build per-ID child contexts.
        var parents = new ParentEntry[contexts.Length];
        Dictionary<string, TypeGroup>? typeGroups = null;

        for (var p = 0; p < contexts.Length; p++)
        {
            var parent = contexts[p];
            int idCount;
            ListValueNode? listIds = null;
            StringValueNode? singleId = null;

            if (parent.ArgumentKind(Ids) == ValueKind.List)
            {
                listIds = parent.ArgumentLiteral<ListValueNode>(Ids);
                idCount = listIds.Items.Count;
            }
            else
            {
                singleId = parent.ArgumentLiteral<StringValueNode>(Ids);
                idCount = 1;
            }

            if (idCount > maxAllowedNodes)
            {
                parent.ReportError(
                    ErrorHelper.FetchedToManyNodesAtOnce(parent.Path, maxAllowedNodes, idCount));
                parents[p] = new ParentEntry(null);
                continue;
            }

            var results = new object?[idCount];
            parents[p] = new ParentEntry(results);

            for (var i = 0; i < idCount; i++)
            {
                var nodeId = listIds is not null
                    ? (StringValueNode)listIds.Items[i]
                    : singleId!;
                var deserializedId = serializer.Parse(nodeId.Value, Unsafe.As<Schema>(schema));
                var typeName = deserializedId.TypeName;

                if (!schema.Types.TryGetType<ObjectType>(typeName, out var type)
                    || type.Features.Get<NodeTypeFeature>() is not { NodeResolver: { } nodeResolver })
                {
                    parent.ReportError(ErrorHelper.Relay_NoNodeResolver(typeName, parent.Path));
                    results[i] = null;
                    continue;
                }

                var child = parent.Clone();
                SetLocalContext(child, nodeId, deserializedId, type);
                TryReplaceArguments(child, nodeResolver, Ids, nodeId);

                typeGroups ??= [];
                if (!typeGroups.TryGetValue(typeName, out var group))
                {
                    group = new TypeGroup(type, nodeResolver, []);
                    typeGroups[typeName] = group;
                }
                group.Entries.Add(new ChildEntry(child, p, i));
            }
        }

        if (typeGroups is not null)
        {
            foreach (var group in typeGroups.Values)
            {
                await DispatchTypeGroupAsync(group.Entries, group.Resolver).ConfigureAwait(false);

                for (var k = 0; k < group.Entries.Count; k++)
                {
                    var entry = group.Entries[k];
                    var parent = contexts[entry.ParentIndex];
                    var result = entry.Context.Result;

                    if (result is IError error)
                    {
                        parent.ReportError(error.WithPath(parent.Path.Append(entry.IdIndex)));
                        parents[entry.ParentIndex].Results![entry.IdIndex] = null;
                    }
                    else
                    {
                        parents[entry.ParentIndex].Results![entry.IdIndex] =
                            CoerceResult(result, group.Type, typeConverter);
                    }
                }
            }
        }

        for (var p = 0; p < contexts.Length; p++)
        {
            contexts[p].Result = parents[p].Results;
        }
    }

    private static async Task DispatchTypeGroupAsync(
        List<ChildEntry> group,
        NodeResolverInfo nodeResolver)
    {
        if (nodeResolver.BatchPipeline is { } batchPipeline)
        {
            // Sub-partition by inner BatchPartitionKey, mirroring the engine's recursion for `node`.
            if (nodeResolver.BatchPartitionKey is { } innerPartitioner && group.Count > 1)
            {
                Dictionary<ulong, List<ChildEntry>>? partitions = null;
                var firstKey = innerPartitioner(group[0].Context);

                for (var i = 1; i < group.Count; i++)
                {
                    var key = innerPartitioner(group[i].Context);

                    if (partitions is null)
                    {
                        if (key == firstKey)
                        {
                            continue;
                        }

                        partitions = [];
                        var firstPartition = new List<ChildEntry>(i);
                        for (var j = 0; j < i; j++)
                        {
                            firstPartition.Add(group[j]);
                        }
                        partitions[firstKey] = firstPartition;
                    }

                    if (!partitions.TryGetValue(key, out var partition))
                    {
                        partition = [];
                        partitions[key] = partition;
                    }
                    partition.Add(group[i]);
                }

                if (partitions is null)
                {
                    var slice = ImmutableArray.CreateBuilder<IMiddlewareContext>(group.Count);
                    for (var i = 0; i < group.Count; i++)
                    {
                        slice.Add(group[i].Context);
                    }
                    await batchPipeline(slice.MoveToImmutable()).ConfigureAwait(false);
                    return;
                }

                foreach (var partition in partitions.Values)
                {
                    var slice = ImmutableArray.CreateBuilder<IMiddlewareContext>(partition.Count);
                    for (var i = 0; i < partition.Count; i++)
                    {
                        slice.Add(partition[i].Context);
                    }
                    await batchPipeline(slice.MoveToImmutable()).ConfigureAwait(false);
                }

                return;
            }

            var contextsBuilder = ImmutableArray.CreateBuilder<IMiddlewareContext>(group.Count);
            for (var i = 0; i < group.Count; i++)
            {
                contextsBuilder.Add(group[i].Context);
            }

            await batchPipeline(contextsBuilder.MoveToImmutable()).ConfigureAwait(false);
            return;
        }

        var pipeline = nodeResolver.Pipeline;

        if (group.Count == 1)
        {
            await pipeline(group[0].Context).ConfigureAwait(false);
            return;
        }

        var tasks = new Task[group.Count];
        for (var i = 0; i < group.Count; i++)
        {
            tasks[i] = pipeline(group[i].Context).AsTask();
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task DispatchAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        NodeResolverInfo nodeResolver)
    {
        if (nodeResolver.BatchPipeline is { } batchPipeline)
        {
            await batchPipeline(contexts).ConfigureAwait(false);
            return;
        }

        var pipeline = nodeResolver.Pipeline;

        if (contexts.Length == 1)
        {
            await pipeline(contexts[0]).ConfigureAwait(false);
            return;
        }

        var tasks = ArrayPool<Task>.Shared.Rent(contexts.Length);
        try
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                tasks[i] = pipeline(contexts[i]).AsTask();
            }

            // Wait only on the first contexts.Length slots since the rented buffer may be larger.
            for (var i = 0; i < contexts.Length; i++)
            {
                await tasks[i].ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(tasks, true);
        }
    }

    private static NodeId ResolveNodeId(
        IMiddlewareContext context,
        INodeIdSerializer serializer,
        string argumentName)
    {
        if (context.LocalContextData.TryGetValue(IdValue, out var cached) && cached is NodeId nodeId)
        {
            return nodeId;
        }

        var literal = context.ArgumentLiteral<StringValueNode>(argumentName);
        return serializer.Parse(literal.Value, Unsafe.As<Schema>(context.Schema));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetLocalContext(
        IMiddlewareContext context,
        StringValueNode nodeId,
        NodeId deserializedId,
        ObjectType type)
    {
        context.SetLocalState(WellKnownContextData.NodeId, nodeId.Value);
        context.SetLocalState(InternalId, deserializedId.InternalId);
        context.SetLocalState(InternalType, type);
        context.SetLocalState(InternalTypeName, type.Name);
        context.SetLocalState(IdValue, deserializedId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TryReplaceArguments(
        IMiddlewareContext context,
        NodeResolverInfo nodeResolverInfo,
        string argumentName,
        StringValueNode argumentLiteral)
    {
        if (nodeResolverInfo.Id is not null)
        {
            // If the node resolver is mapped from an actual field resolver,
            // we will create a new argument value since the field resolvers argument could
            // have a different type and argument name.
            var idArg = new ArgumentValue(
                nodeResolverInfo.Id,
                ValueKind.String,
                false,
                false,
                null,
                argumentLiteral);

            // Note that in standard middleware we should restore the original
            // argument after we have invoked the next pipeline element.
            // However, the node field is under our control, and we can guarantee
            // that there is no other middleware involved and allowed,
            // meaning we skip the restore.
            context.ReplaceArgument(argumentName, idArg);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? CoerceResult(
        object? result,
        ObjectType type,
        ITypeConverter typeConverter)
    {
        if (result is null || result is IError || type.RuntimeType.IsInstanceOfType(result))
        {
            return result;
        }

        return typeConverter.TryConvert(type.RuntimeType, result, out var converted)
            ? converted
            : result;
    }

    private readonly record struct ParentEntry(object?[]? Results);

    private readonly record struct ChildEntry(
        IMiddlewareContext Context,
        int ParentIndex,
        int IdIndex);

    private sealed record TypeGroup(
        ObjectType Type,
        NodeResolverInfo Resolver,
        List<ChildEntry> Entries);
}
