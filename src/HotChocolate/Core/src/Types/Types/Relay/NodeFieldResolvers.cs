#nullable enable

using System.Buffers;
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
    private static readonly Task<object?> _nullTask = Task.FromResult<object?>(null);

    /// <summary>
    /// This is the resolver of the node field.
    /// </summary>
    public static async ValueTask ResolveSingleNodeAsync(
        IMiddlewareContext context,
        INodeIdSerializer serializer)
    {
        var nodeId = context.ArgumentLiteral<StringValueNode>(Id);
        var deserializedId = serializer.Parse(nodeId.Value, context.Schema);
        var typeName = deserializedId.TypeName;

        // if the type has a registered node resolver we will execute it.
        if (context.Schema.TryGetType<ObjectType>(typeName, out var type) &&
            type.ContextData.TryGetValue(NodeResolver, out var o) &&
            o is NodeResolverInfo nodeResolverInfo)
        {
            SetLocalContext(context, nodeId, deserializedId, type);
            TryReplaceArguments(context, nodeResolverInfo, Id, nodeId);

            await nodeResolverInfo.Pipeline.Invoke(context);
        }
        else
        {
            context.ReportError(
                ErrorHelper.Relay_NoNodeResolver(
                    typeName,
                    context.Path,
                    context.Selection.SyntaxNodes));

            context.Result = null;
        }
    }

    /// <summary>
    /// This is the resolver of the nodes field.
    /// </summary>
    public static async ValueTask ResolveManyNodeAsync(
        IMiddlewareContext context,
        INodeIdSerializer serializer,
        int maxAllowedNodes)
    {
        var schema = context.Schema;

        if (context.ArgumentKind(Ids) == ValueKind.List)
        {
            var list = context.ArgumentLiteral<ListValueNode>(Ids);

            if (list.Items.Count > maxAllowedNodes)
            {
                context.ReportError(
                    ErrorHelper.FetchedToManyNodesAtOnce(
                        context.Selection.SyntaxNodes,
                        context.Path,
                        maxAllowedNodes,
                        list.Items.Count));
                return;
            }

            var tasks = ArrayPool<Task<object?>>.Shared.Rent(list.Items.Count);
            var result = new object?[list.Items.Count];
            var ct = context.RequestAborted;

            for (var i = 0; i < list.Items.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var nodeId = (StringValueNode)list.Items[i];
                var deserializedId = serializer.Parse(nodeId.Value, context.Schema);
                var typeName = deserializedId.TypeName;

                // if the type has a registered node resolver we will execute it.
                if (schema.TryGetType<ObjectType>(typeName, out var type) &&
                    type.ContextData.TryGetValue(NodeResolver, out var o) &&
                    o is NodeResolverInfo nodeResolverInfo)
                {
                    var nodeContext = context.Clone();

                    SetLocalContext(nodeContext, nodeId, deserializedId, type);
                    TryReplaceArguments(nodeContext, nodeResolverInfo, Ids, nodeId);

                    tasks[i] = ExecutePipelineAsync(nodeContext, nodeResolverInfo);
                }
                else
                {
                    tasks[i] = _nullTask;

                    context.ReportError(
                        ErrorHelper.Relay_NoNodeResolver(
                            typeName,
                            context.Path,
                            context.Selection.SyntaxNodes));
                }
            }

            for (var i = 0; i < list.Items.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var task = tasks[i];

                if (task.IsCompleted)
                {
                    if (task.Exception is null)
                    {
                        result[i] = task.Result;
                    }
                    else
                    {
                        result[i] = null;
                        ReportError(context, i, task.Exception);
                    }
                }
                else
                {
                    try
                    {
                        result[i] = await task;
                    }
                    catch (Exception ex)
                    {
                        result[i] = null;
                        ReportError(context, i, ex);
                    }
                }
            }

            context.Result = result;
            ArrayPool<Task<object?>>.Shared.Return(tasks, true);
        }
        else
        {
            var result = new object?[1];
            var nodeId = context.ArgumentLiteral<StringValueNode>(Ids);
            var deserializedId = serializer.Parse(nodeId.Value, context.Schema);
            var typeName = deserializedId.TypeName;

            // if the type has a registered node resolver we will execute it.
            if (schema.TryGetType<ObjectType>(typeName, out var type) &&
                type.ContextData.TryGetValue(NodeResolver, out var o) &&
                o is NodeResolverInfo nodeResolverInfo)
            {
                var nodeContext = context.Clone();

                SetLocalContext(nodeContext, nodeId, deserializedId, type);
                TryReplaceArguments(nodeContext, nodeResolverInfo, Ids, nodeId);

                result[0] = await ExecutePipelineAsync(nodeContext, nodeResolverInfo);
            }
            else
            {
                result[0] = null;

                context.ReportError(
                    ErrorHelper.Relay_NoNodeResolver(
                        typeName,
                        context.Path,
                        context.Selection.SyntaxNodes));
            }

            context.Result = result;
        }
        return;

        static async Task<object?> ExecutePipelineAsync(
            IMiddlewareContext nodeResolverContext,
            NodeResolverInfo nodeResolverInfo)
        {
            await nodeResolverInfo.Pipeline.Invoke(nodeResolverContext).ConfigureAwait(false);
            return nodeResolverContext.Result;
        }
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
            // If the node resolver is a mapped from an actual field resolver,
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
            // that there are no other middleware involved and allowed,
            // meaning we skip the restore.
            context.ReplaceArgument(argumentName, idArg);
        }
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
        => context.ReportError(ex, error => error.SetPath(context.Path.Append(item)));
}
