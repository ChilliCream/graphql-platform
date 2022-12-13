#nullable enable

using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.Types.Relay.NodeFieldResolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This type interceptor adds the fields `node` and the `nodes` to the query type.
/// </summary>
internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if ((completionContext.IsQueryType ?? false) &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            var typeInspector = completionContext.TypeInspector;

            var serializer =
                completionContext.Services.GetService<IIdSerializer>() ??
                new IdSerializer();

            // the nodes fields shall be chained in after the introspection fields,
            // so we first get the index of the last introspection field,
            // which is __typename
            var typeNameField = objectTypeDefinition.Fields.First(
                t => t.Name.EqualsOrdinal(IntrospectionFields.TypeName) &&
                    t.IsIntrospectionField);
            var index = objectTypeDefinition.Fields.IndexOf(typeNameField);

            CreateNodeField(typeInspector, serializer, objectTypeDefinition.Fields, index + 1);
            CreateNodesField(typeInspector, serializer, objectTypeDefinition.Fields, index + 2);
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
            node)
        {
            Arguments =
            {
                new ArgumentDefinition(Id, Relay_NodeField_Id_Description, id)
            },
            MiddlewareDefinitions =
            {
                new FieldMiddlewareDefinition(
                    next => async context =>
                    {
                        await ResolveSingleNodeAsync(context, serializer).ConfigureAwait(false);
                        await next(context).ConfigureAwait(false);
                    })
            }
        };

        // In the projection interceptor we want to change the context data that is on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it to be replaced with ExtensionData.Empty
        field.ContextData[WellKnownContextData.IsNodeField] = true;

        fields.Insert(index, field);
    }

    private static void CreateNodesField(
        ITypeInspector typeInspector,
        IIdSerializer serializer,
        IList<ObjectFieldDefinition> fields,
        int index)
    {
        var nodes = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NodeType>>));
        var ids = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NonNullType<IdType>>>));

        var field = new ObjectFieldDefinition(
            Nodes,
            Relay_NodesField_Description,
            nodes)
        {
            Arguments =
            {
                new ArgumentDefinition(Ids, Relay_NodesField_Ids_Description, ids)
            },
            MiddlewareDefinitions =
            {
                new FieldMiddlewareDefinition(
                    next => async context =>
                    {
                        await ResolveManyNodeAsync(context, serializer).ConfigureAwait(false);
                        await next(context).ConfigureAwait(false);
                    })
            }
        };

        // In the projection interceptor we want to change the context data that is on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it to be replaced with ExtensionData.Empty
        field.ContextData[WellKnownContextData.IsNodesField] = true;

        fields.Insert(index, field);
    }
}
