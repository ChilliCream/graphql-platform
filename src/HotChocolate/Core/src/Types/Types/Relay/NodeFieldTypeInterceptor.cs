#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.Types.Relay.NodeFieldResolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This type interceptor adds the fields `node` and the `nodes` to the query type.
/// </summary>
internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
{
    private ITypeCompletionContext? _queryContext;
    private ObjectTypeDefinition? _queryTypeDefinition;

    internal override uint Position => uint.MaxValue - 100;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
            _queryTypeDefinition = definition;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryContext is not null && _queryTypeDefinition is not null)
        {
            var typeInspector = _queryContext.TypeInspector;

            var serializer = _queryContext.DescriptorContext.NodeIdSerializerAccessor;

            // the nodes fields shall be chained in after the introspection fields,
            // so we first get the index of the last introspection field,
            // which is __typename
            var typeNameField = _queryTypeDefinition.Fields.First(
                t => t.Name.EqualsOrdinal(IntrospectionFields.TypeName) &&
                    t.IsIntrospectionField);
            var index = _queryTypeDefinition.Fields.IndexOf(typeNameField);
            var maxAllowedNodes = _queryContext.DescriptorContext.Options.MaxAllowedNodeBatchSize;

            CreateNodeField(
                typeInspector,
                serializer,
                _queryTypeDefinition.Fields,
                index + 1);

            CreateNodesField(
                typeInspector,
                serializer,
                _queryTypeDefinition.Fields,
                index + 2,
                maxAllowedNodes);
        }
    }

    private static void CreateNodeField(
        ITypeInspector typeInspector,
        INodeIdSerializerAccessor serializerAccessor,
        IList<ObjectFieldDefinition> fields,
        int index)
    {
        var node = typeInspector.GetTypeRef(typeof(NodeType));
        var id = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));

        var field = new ObjectFieldDefinition(
            Node,
            Relay_NodeField_Description,
            node)
        {
            Arguments = { new ArgumentDefinition(Id, Relay_NodeField_Id_Description, id), },
            MiddlewareDefinitions =
            {
                new FieldMiddlewareDefinition(
                    _ =>
                    {
                        INodeIdSerializer? serializer = null;
                        return async context =>
                        {
                            serializer ??= serializerAccessor.Serializer;
                            await ResolveSingleNodeAsync(context, serializer).ConfigureAwait(false);
                        };
                    }),
            },
            Flags = FieldFlags.ParallelExecutable | FieldFlags.GlobalIdNodeField
        };

        // In the projection interceptor we want to change the context data that is on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it to be replaced with ExtensionData.Empty
        field.ContextData[WellKnownContextData.IsNodeField] = true;

        fields.Insert(index, field);
    }

    private static void CreateNodesField(
        ITypeInspector typeInspector,
        INodeIdSerializerAccessor serializerAccessor,
        IList<ObjectFieldDefinition> fields,
        int index,
        int maxAllowedNodes)
    {
        var nodes = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NodeType>>));
        var ids = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NonNullType<IdType>>>));

        var field = new ObjectFieldDefinition(
            Nodes,
            Relay_NodesField_Description,
            nodes)
        {
            Arguments = { new ArgumentDefinition(Ids, Relay_NodesField_Ids_Description, ids), },
            MiddlewareDefinitions =
            {
                new FieldMiddlewareDefinition(
                    _ =>
                    {
                        INodeIdSerializer? serializer = null;
                        return async context =>
                        {
                            serializer ??= serializerAccessor.Serializer;
                            await ResolveManyNodeAsync(context, serializer, maxAllowedNodes).ConfigureAwait(false);
                        };
                    }),
            },
            Flags = FieldFlags.ParallelExecutable | FieldFlags.GlobalIdNodesField
        };

        // In the projection interceptor we want to change the context data that is on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it to be replaced with ExtensionData.Empty
        field.ContextData[WellKnownContextData.IsNodesField] = true;

        fields.Insert(index, field);
    }
}
