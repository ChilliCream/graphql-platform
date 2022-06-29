#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.WellKnownContextData;

namespace HotChocolate.Types.Relay;

internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
{
    private static readonly Task<object?> _nullTask = Task.FromResult<object?>(null);
    private static string Node => "node";
    private static string Nodes => "nodes";
    private static string Id => "id";
    private static string Ids => "ids";

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if ((completionContext.IsQueryType ?? false) &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            var typeInspector = completionContext.TypeInspector;

            var serializer =
                completionContext.Services.GetService<IIdSerializer>() ??
                new IdSerializer();

            var typeNameField = objectTypeDefinition.Fields.First(
                t => t.Name.EqualsOrdinal(IntrospectionFields.TypeName) &&
                    t.IsIntrospectionField);
            var index = objectTypeDefinition.Fields.IndexOf(typeNameField);

            CreateNodeField(typeInspector, serializer, objectTypeDefinition.Fields, index + 1);
            CreateNodesField(serializer, objectTypeDefinition.Fields, index + 2);
        }
    }

    private static void CreateNodeField(
        ITypeInspector typeInspector,
        IIdSerializer serializer,
        IList<ObjectFieldDefinition> fields,
        int index)
    {
        var node = typeInspector.GetTypeRef(typeof(NodeType));
        var id = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));

        var field = new ObjectFieldDefinition(
            Node,
            Relay_NodeField_Description,
            node,
            ResolveNodeAsync) { Arguments = { new(Id, Relay_NodeField_Id_Description, id) } };

        fields.Insert(index, field);

        ValueTask<object?> ResolveNodeAsync(IResolverContext ctx)
            => ResolveSingleNode(ctx, serializer, Id);
    }

    private static void CreateNodesField(
        IIdSerializer serializer,
        IList<ObjectFieldDefinition> fields,
        int index)
    {
        var nodes = TypeReference.Parse("[Node]!");
        var ids = TypeReference.Parse("[ID!]!");

        var field = new ObjectFieldDefinition(
            Nodes,
            Relay_NodesField_Description,
            nodes,
            ResolveNodeAsync) { Arguments = { new(Ids, Relay_NodesField_Ids_Description, ids) } };

        fields.Insert(index, field);

        ValueTask<object?> ResolveNodeAsync(IResolverContext ctx)
            => ResolveManyNode(ctx, serializer);
    }

    private static async ValueTask<object?> ResolveSingleNode(
        IResolverContext context,
        IIdSerializer serializer,
        string argumentName)
    {
        var nodeId = context.ArgumentLiteral<StringValueNode>(argumentName);
        var deserializedId = serializer.Deserialize(nodeId.Value);
        var typeName = deserializedId.TypeName;

        context.SetLocalState(NodeId, nodeId.Value);
        context.SetLocalState(InternalId, deserializedId.Value);
        context.SetLocalState(InternalType, typeName);
        context.SetLocalState(WellKnownContextData.IdValue, deserializedId);

        if (context.Schema.TryGetType<ObjectType>(typeName!, out var type) &&
            type.ContextData.TryGetValue(NodeResolver, out var o) &&
            o is FieldResolverDelegate resolver)
        {
            return await resolver.Invoke(context).ConfigureAwait(false);
        }

        return null;
    }

    private static async ValueTask<object?> ResolveManyNode(
        IResolverContext context,
        IIdSerializer serializer)
    {
        if (context.ArgumentKind(Ids) == ValueKind.List)
        {
            var list = context.ArgumentLiteral<ListValueNode>(Ids);
            var tasks = ArrayPool<Task<object?>>.Shared.Rent(list.Items.Count);
            var result = new object?[list.Items.Count];

            try
            {
                for (var i = 0; i < list.Items.Count; i++)
                {
                    context.RequestAborted.ThrowIfCancellationRequested();

                    // it is guaranteed that this is always a string literal.
                    var nodeId = (StringValueNode)list.Items[i];
                    var deserializedId = serializer.Deserialize(nodeId.Value);
                    var typeName = deserializedId.TypeName;

                    context.SetLocalState(NodeId, nodeId.Value);
                    context.SetLocalState(InternalId, deserializedId.Value);
                    context.SetLocalState(InternalType, typeName);
                    context.SetLocalState(WellKnownContextData.IdValue, deserializedId);

                    tasks[i] =
                        context.Schema.TryGetType<ObjectType>(typeName!, out var type) &&
                        type.ContextData.TryGetValue(NodeResolver, out var o) &&
                        o is FieldResolverDelegate resolver
                            ? resolver.Invoke(new ResolverContextProxy(context)).AsTask()
                            : _nullTask;
                }

                for (var i = 0; i < list.Items.Count; i++)
                {
                    context.RequestAborted.ThrowIfCancellationRequested();

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

                return result;
            }
            finally
            {
                ArrayPool<Task<object?>>.Shared.Return(tasks, true);
            }
        }
        else
        {
            var result = new object?[1];
            result[0] = await ResolveSingleNode(context, serializer, Ids);
            return result;
        }
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
    {
        Path itemPath = PathFactory.Instance.Append(context.Path, item);
        context.ReportError(ex, error => error.SetPath(itemPath));
    }
}
