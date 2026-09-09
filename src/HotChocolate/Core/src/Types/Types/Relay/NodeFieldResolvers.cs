using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
using System.Runtime.ExceptionServices;
#endif
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
        if (contexts.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException(
                "A batch resolver is guaranteed to have at least one context.");
        }

        var serializer = serializerAccessor.Serializer;
        var first = contexts[0];
        var schema = first.Schema;
        var typeConverter = first.Service<ITypeConverter>();

        // The engine composes the type name into the partition key, so a slice is homogeneous
        // only up to a 64-bit hash collision. The dominant case is a single context, or all
        // contexts resolving to the same type without a malformed id, so we scan once and, when
        // the slice is homogeneous, dispatch the original contexts array without any grouping
        // allocation. Only on a malformed id or a type mismatch do we fall back to grouping the
        // contexts by their resolved type so a malformed id cannot poison its valid siblings.
        ObjectType? homogeneousType = null;
        NodeResolverInfo? homogeneousResolver = null;
        Dictionary<string, NodeContextGroup>? groups = null;

        for (var i = 0; i < contexts.Length; i++)
        {
            var ctx = contexts[i];

            if (!TryResolveNodeId(ctx, serializer, Id, out var deserializedId, out var parseError))
            {
                groups ??= BuildGroups(contexts, i, homogeneousType, homogeneousResolver);
                ctx.ReportError(parseError);
                ctx.Result = null;
                continue;
            }

            var typeName = deserializedId.TypeName;

            if (!schema.Types.TryGetType<ObjectType>(typeName, out var type)
                || type.Features.Get<NodeTypeFeature>() is not { NodeResolver: { } nodeResolver })
            {
                groups ??= BuildGroups(contexts, i, homogeneousType, homogeneousResolver);
                ctx.ReportError(ErrorHelper.Relay_NoNodeResolver(typeName, ctx.Path));
                ctx.Result = null;
                continue;
            }

            var nodeId = ctx.ArgumentLiteral<StringValueNode>(Id);
            SetLocalContext(ctx, nodeId, deserializedId, type);
            TryReplaceArguments(ctx, nodeResolver, Id, nodeId);

            if (groups is null
                && (homogeneousType is null || ReferenceEquals(homogeneousType, type)))
            {
                homogeneousType = type;
                homogeneousResolver = nodeResolver;
                continue;
            }

            // A second type appeared, so the slice is not homogeneous and we switch to grouping.
            groups ??= BuildGroups(contexts, i, homogeneousType, homogeneousResolver);
            AddToGroup(groups, typeName, type, nodeResolver, ctx);
        }

        if (groups is null)
        {
            if (homogeneousType is null)
            {
                return;
            }

            await DispatchAsync(contexts, homogeneousResolver!).ConfigureAwait(false);

            for (var i = 0; i < contexts.Length; i++)
            {
                var ctx = contexts[i];
                ctx.Result = CoerceResult(ctx.Result, homogeneousType, typeConverter);
            }

            return;
        }

        foreach (var group in groups.Values)
        {
            var slice = ImmutableArray.CreateBuilder<IMiddlewareContext>(group.Contexts.Count);
            for (var i = 0; i < group.Contexts.Count; i++)
            {
                slice.Add(group.Contexts[i]);
            }

            await DispatchAsync(slice.MoveToImmutable(), group.Resolver).ConfigureAwait(false);

            for (var i = 0; i < group.Contexts.Count; i++)
            {
                var ctx = group.Contexts[i];
                ctx.Result = CoerceResult(ctx.Result, group.Type, typeConverter);
            }
        }
    }

    private static Dictionary<string, NodeContextGroup> BuildGroups(
        ImmutableArray<IMiddlewareContext> contexts,
        int count,
        ObjectType? homogeneousType,
        NodeResolverInfo? homogeneousResolver)
    {
        // The fast path resolved the ids of the first count contexts as a single homogeneous
        // type. We seed the groups with those contexts so the grouping path can take over.
        var groups = new Dictionary<string, NodeContextGroup>();

        if (homogeneousType is not null)
        {
            for (var i = 0; i < count; i++)
            {
                AddToGroup(groups, homogeneousType.Name, homogeneousType, homogeneousResolver!, contexts[i]);
            }
        }

        return groups;
    }

    private static void AddToGroup(
        Dictionary<string, NodeContextGroup> groups,
        string typeName,
        ObjectType type,
        NodeResolverInfo nodeResolver,
        IMiddlewareContext ctx)
    {
        if (!groups.TryGetValue(typeName, out var group))
        {
            group = new NodeContextGroup(type, nodeResolver, []);
            groups[typeName] = group;
        }
        group.Contexts.Add(ctx);
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
        List<StagedChild>? staged = null;

        for (var p = 0; p < contexts.Length; p++)
        {
            var parent = contexts[p];

            // Sibling aliased nodes fields share a single batch context, so a malformed id or an
            // incompatible id literal in one field must not poison its valid siblings. We read the
            // argument, parse, and expand a parent's full id list inside the per-context try, and,
            // mirroring the per-field semantics, error the whole field if any id is malformed while
            // leaving the other parent contexts untouched.
            object?[]? results = null;

            try
            {
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

                results = new object?[idCount];
                staged?.Clear();

                for (var i = 0; i < idCount; i++)
                {
                    StringValueNode nodeId;
                    if (listIds is not null)
                    {
                        // An id literal that passed validation but is not a string (for example an
                        // int inside the list) must surface the same literal-not-compatible error
                        // as a top-level non-string id, not a masked cast exception.
                        if (listIds.Items[i] is not StringValueNode stringItem)
                        {
                            throw Execution.ThrowHelper.ResolverContext_LiteralNotCompatible(
                                parent.Selection.SyntaxNodes[0].Node,
                                parent.Path,
                                Ids,
                                typeof(StringValueNode),
                                listIds.Items[i].GetType());
                        }

                        nodeId = stringItem;
                    }
                    else
                    {
                        nodeId = singleId!;
                    }

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

                    staged ??= [];
                    staged.Add(new StagedChild(typeName, type, nodeResolver, child, i));
                }
            }
            catch (Exception ex)
            {
                parent.ReportError(ex);
                parent.Result = null;
                parents[p] = new ParentEntry(null);
                continue;
            }

            parents[p] = new ParentEntry(results);

            if (staged is null)
            {
                continue;
            }

            for (var s = 0; s < staged.Count; s++)
            {
                var entry = staged[s];

                typeGroups ??= [];
                if (!typeGroups.TryGetValue(entry.TypeName, out var group))
                {
                    group = new TypeGroup(entry.Type, entry.Resolver, []);
                    typeGroups[entry.TypeName] = group;
                }
                group.Entries.Add(new ChildEntry(entry.Context, p, entry.IdIndex));
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
            // The nodes field has no engine-level partitioner, so when the node resolver exposes
            // an inner partition key we sub-partition the per-type group here by that key.
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

        var tasks = ArrayPool<Task>.Shared.Rent(group.Count);

        try
        {
            for (var i = 0; i < group.Count; i++)
            {
                tasks[i] = pipeline(group[i].Context).AsTask();
            }

#if NET9_0_OR_GREATER
            await Task.WhenAll(tasks.AsSpan(0, group.Count)).ConfigureAwait(false);
#else
            await ObserveAllAsync(tasks, group.Count).ConfigureAwait(false);
#endif
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(tasks, true);
        }
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

#if NET9_0_OR_GREATER
            await Task.WhenAll(tasks.AsSpan(0, contexts.Length)).ConfigureAwait(false);
#else
            await ObserveAllAsync(tasks, contexts.Length).ConfigureAwait(false);
#endif
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(tasks, true);
        }
    }

#if !NET9_0_OR_GREATER
    private static async Task ObserveAllAsync(Task[] tasks, int count)
    {
        // Every task must be awaited before we rethrow so that no still-running pipeline
        // writes to a context that has already been returned to the pool.
        ExceptionDispatchInfo? captured = null;

        for (var i = 0; i < count; i++)
        {
            try
            {
                await tasks[i].ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                captured ??= ExceptionDispatchInfo.Capture(ex);
            }
        }

        captured?.Throw();
    }
#endif

    private static bool TryResolveNodeId(
        IMiddlewareContext context,
        INodeIdSerializer serializer,
        string argumentName,
        out NodeId deserializedId,
        out Exception parseError)
    {
        // In a multi-context batch the engine partitioner already validated and cached the id of
        // every surviving context, so this read returns the cached value and the try/catch below
        // never runs. In a single-context batch the partitioner never runs (it is guarded on more
        // than one context), so this try/catch is the path that parses the id and reports a
        // per-context error for a malformed or incompatible id.
        if (context.LocalContextData.TryGetValue(IdValue, out var cached) && cached is NodeId nodeId)
        {
            deserializedId = nodeId;
            parseError = null!;
            return true;
        }

        try
        {
            // The argument read is inside the try so that an id literal that passed validation
            // but is not a string surfaces as a per-context error instead of throwing across the
            // batch and poisoning sibling contexts.
            var literal = context.ArgumentLiteral<StringValueNode>(argumentName);
            deserializedId = serializer.Parse(literal.Value, Unsafe.As<Schema>(context.Schema));
        }
        catch (Exception ex)
        {
            deserializedId = default;
            parseError = ex;
            return false;
        }

        parseError = null!;
        return true;
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

    private readonly record struct StagedChild(
        string TypeName,
        ObjectType Type,
        NodeResolverInfo Resolver,
        IMiddlewareContext Context,
        int IdIndex);

    private sealed record TypeGroup(
        ObjectType Type,
        NodeResolverInfo Resolver,
        List<ChildEntry> Entries);

    private sealed record NodeContextGroup(
        ObjectType Type,
        NodeResolverInfo Resolver,
        List<IMiddlewareContext> Contexts);
}
