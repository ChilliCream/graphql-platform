#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            ctx => ResolveSingleNode(ctx, serializer, Id))
        {
            Arguments =
            {
                new ArgumentDefinition(Id, Relay_NodeField_Id_Description, id)
            }
        };

        fields.Insert(index, field);
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
            ctx => ResolveManyNode(ctx, serializer))
        {
            Arguments =
            {
                new ArgumentDefinition(Ids, Relay_NodesField_Ids_Description, ids)
            }
        };

        fields.Insert(index, field);
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

        if (context.Schema.TryGetType<ObjectType>(typeName, out var type) &&
            type.ContextData.TryGetValue(NodeResolver, out var o))
        {
            if (o is FieldResolverDelegate resolver)
            {
                return await resolver.Invoke(context).ConfigureAwait(false);
            }

            if (o is FieldDelegate pipeline)
            {
                var m = (IMiddlewareContext)context;
                await pipeline.Invoke(m);
                return m.Result;
            }
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

internal sealed class NodeResolverTypeInterceptor : TypeInterceptor
{
    private readonly List<IDictionary<string, object?>> _nodes = new();
    private ObjectType? _queryType;

    public override bool TriggerAggregations => true;

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType,
        IDictionary<string, object?> contextData)
    {
        // we are only interested in the query type to infer node resolvers
        // from the specified query fields.
        if ((completionContext.IsQueryType ?? false) &&
            definition is ObjectTypeDefinition typeDef &&
            completionContext.Type is ObjectType queryType)
        {
            _queryType = queryType;

            foreach (var fieldDef in typeDef.Fields)
            {
                var resolverMember = fieldDef.ResolverMember ?? fieldDef.Member;

                // candidate fields that we might be able to use as node resolvers must specify
                // a resolver member. Delegates or expressions are not supported.
                // Further, we only will look at annotated fields. This feature is always opt-in.
                if (fieldDef.Type is not null &&
                    resolverMember is not null &&
                    resolverMember.IsDefined(typeof(NodeResolverAttribute)))
                {
                    // Query fields that users want to reuse as node resolvers must exactly specify
                    // one argument that represents the node id.
                    if (fieldDef.Arguments.Count != 1)
                    {
                        // todo: error helper
                        completionContext.ReportError(
                            SchemaErrorBuilder
                                .New()
                                .SetMessage("A node resolver must specify exactly one id argument.")
                                .Build());
                        continue;
                    }

                    var argument = fieldDef.Arguments[0];

                    if (argument.Type is null)
                    {
                        // todo: throw helper
                        throw new InvalidOperationException(
                            "A field argument at this initialization state is guaranteed to have an argument type, but we found none.");
                    }

                    var fieldType = completionContext.GetType<IType>(fieldDef.Type);

                    if (!fieldType.IsObjectType())
                    {
                        // todo: error helper
                        completionContext.ReportError(
                            SchemaErrorBuilder
                                .New()
                                .SetMessage("A node resolver must return an object type.")
                                .Build());
                        continue;
                    }

                    var fieldTypeDef = ((ObjectType)fieldType.NamedType()).Definition;

                    if (fieldTypeDef is null)
                    {
                        // todo: throw helper
                        throw new InvalidOperationException(
                            "An object type at this point is guaranteed to have a type definition, but we found none.");
                    }

                    var idDef = fieldTypeDef.Fields.FirstOrDefault(t => t.Name.EqualsOrdinal("id"));

                    if (idDef is null)
                    {
                        // todo: error helper
                        completionContext.ReportError(
                            SchemaErrorBuilder
                                .New()
                                .SetMessage("A type implementing the node interface must expose an id field.")
                                .Build());
                        continue;
                    }

                    // we will ensure that the object type is implementing the node type interface.
                    fieldTypeDef.Interfaces.Add(TypeReference.Parse("Node"));
                    fieldTypeDef.ContextData[NodeResolver] = fieldDef.Name;

                    // TODO : the typename is not guaranteed yet.
                    // We will ensure that the node id argument is always a non-null ID type.
                    argument.Type = TypeReference.Parse("ID!");
                    RelayIdFieldHelpers.AddSerializerToInputField(completionContext, argument, fieldTypeDef.Name);

                    idDef.Type = argument.Type;
                    RelayIdFieldHelpers.ApplyIdToField(idDef);

                    _nodes.Add(fieldTypeDef.ContextData);
                }
            }
        }
    }

    public override void OnAfterCompleteTypes()
    {
        if (_queryType is not null && _nodes.Count > 0)
        {
            foreach (var node in _nodes)
            {
                var fieldName = (string)node[NodeResolver]!;
                node[NodeResolver] = _queryType.Fields[fieldName].Middleware;
            }
        }
    }
}
