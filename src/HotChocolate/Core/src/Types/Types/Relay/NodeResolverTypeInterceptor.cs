#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Utilities.ErrorHelper;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This type interceptor inspects the query type to look for fields that double as node resolvers.
/// </summary>
internal sealed class NodeResolverTypeInterceptor : TypeInterceptor
{
    private readonly List<IDictionary<string, object?>> _nodes = [];

    internal override uint Position => uint.MaxValue - 101;

    private ITypeCompletionContext? CompletionContext { get; set; }

    private ObjectType? QueryType { get; set; }

    private ObjectTypeDefinition? TypeDef { get; set; }

    [MemberNotNullWhen(true, nameof(QueryType), nameof(TypeDef), nameof(CompletionContext))]
    private bool IsInitialized
        => QueryType is not null &&
            TypeDef is not null &&
            CompletionContext is not null;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        // we are only interested in the query type to infer node resolvers.
        if (operationType is OperationType.Query &&
            completionContext.Type is ObjectType queryType)
        {
            CompletionContext = completionContext;
            TypeDef = definition;
            QueryType = queryType;
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        if (!IsInitialized)
        {
            return;
        }

        // we store the query types as state on the type interceptor,
        // so that we can use it to get the final field resolver pipeline
        // form the query fields that double as node resolver once they are
        // fully compiled.
        var typeInspector = CompletionContext.TypeInspector;

        foreach (var fieldDef in TypeDef.Fields)
        {
            var resolverMember = fieldDef.ResolverMember ?? fieldDef.Member;

            // candidate fields that we might be able to use as node resolvers must specify
            // a resolver member. Delegates or expressions are not supported as node resolvers.
            // Further, we only will look at annotated fields. This feature is always opt-in.
            if (fieldDef.Type is not null &&
                resolverMember is not null &&
                fieldDef.Expression is null &&
                resolverMember.IsDefined(typeof(NodeResolverAttribute)))
            {
                // Query fields that users want to reuse as node resolvers must exactly specify
                // one argument and that argument must be the node id.
                if (fieldDef.Arguments.Count != 1)
                {
                    CompletionContext.ReportError(
                        NodeResolver_MustHaveExactlyOneIdArg(
                            fieldDef.Name,
                            QueryType));
                    continue;
                }

                // We will capture the argument and ensure that it has a type.
                // If ut does not have a type something is wrong with the initialization
                // process and we will fail the initialization.
                var argument = fieldDef.Arguments[0];

                if (argument.Type is null)
                {
                    throw NodeResolver_ArgumentTypeMissing();
                }

                // Next we will capture the field result type and ensure that it is an
                // object type.
                // Node resolvers can only be object types.
                // Interfaces, unions are not allowed as we resolve a concrete node type.
                // Also we cannot use resolvers that return a list or really anything else
                // then an object type.
                var fieldType = CompletionContext.GetType<IType>(fieldDef.Type);

                if (!fieldType.IsObjectType())
                {
                    CompletionContext.ReportError(
                        NodeResolver_MustReturnObject(
                            fieldDef.Name,
                            QueryType));
                    continue;
                }

                // Once we have the type instance we need to grab it type definition to
                // inject a placeholder for the node resolver pipeline into the types
                // context data.
                var fieldTypeDef = ((ObjectType)fieldType.NamedType()).Definition;

                if (fieldTypeDef is null)
                {
                    throw NodeResolver_ObjNoDefinition();
                }

                // Before we go any further we will ensure that the type either implements the
                // node interface already or it contains an id field.
                if (!ImplementsNode(CompletionContext, TypeDef))
                {
                    // we will ensure that the object type is implementing the node type interface.
                    fieldTypeDef.Interfaces.Add(typeInspector.GetTypeRef(typeof(NodeType)));
                }

                var idDef = fieldTypeDef.Fields.FirstOrDefault(t => t.Name.EqualsOrdinal(Id));

                if (idDef is null)
                {
                    CompletionContext.ReportError(
                        NodeResolver_NodeTypeHasNoId(
                            (ObjectType)fieldType.NamedType()));
                    continue;
                }

                // Now that we know we can infer a node resolver form the annotated query field
                // we will start mutating the type and field.
                // First we are adding a marker to the node type`s context data.
                // We will replace this later with a NodeResolverInfo instance that
                // allows the node field to resolve a node instance by its ID.
                fieldTypeDef.ContextData[NodeResolver] = fieldDef.Name;

                // We also want to ensure that the node id argument is always a non-null
                // ID type. So, if the user has not specified that we are making sure of this
                // by overwriting the arguments type reference.
                argument.Type = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));

                // We also need to add an input formatter to the argument the decodes passed
                // in ID values.
                RelayIdFieldHelpers.AddSerializerToInputField(
                    CompletionContext,
                    argument,
                    fieldTypeDef.Name);

                // As with the id argument we also want to make sure that the ID field of
                // the fields result type is a non-null ID type.
                idDef.Type = argument.Type;

                // For the id field we need to make sure that a result formatter is registered
                // that encodes the IDs returned from the id field.
                RelayIdFieldHelpers.ApplyIdToField(idDef);

                // Last we register the context data of our node with the type
                // interceptors state.
                // We do that to replace our marker with the actual NodeResolverInfo instance.
                _nodes.Add(fieldTypeDef.ContextData);
            }
        }
    }

    public override void OnAfterCompleteTypes()
    {
        if (QueryType is not null && _nodes.Count > 0)
        {
            // After all types are completed it is guaranteed that all
            // query field resolver pipelines are fully compiled.
            // So, we can start replacing our marker with the actual NodeResolverInfo.
            foreach (var node in _nodes)
            {
                var fieldName = (string)node[NodeResolver]!;
                var field = QueryType.Fields[fieldName];

                node[NodeResolver] = new NodeResolverInfo(field, field.Middleware);
            }
        }
    }

    private static bool ImplementsNode(
        ITypeCompletionContext context,
        ObjectTypeDefinition typeDef)
    {
        if (typeDef.Interfaces.Count > 0)
        {
            foreach (var interfaceRef in typeDef.Interfaces)
            {
                if (context.TryGetType<InterfaceType>(interfaceRef, out var type) &&
                    type.Name.Equals(NodeType.Names.Node))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
