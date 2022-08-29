#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.Types.WellKnownContextData;
using static HotChocolate.Utilities.ErrorHelper;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This node resolver inspects the query type for fields that double as node resolvers.
/// </summary>
internal sealed class NodeResolverTypeInterceptor : TypeInterceptor
{
    private readonly List<IDictionary<string, object?>> _nodes = new();
    private ObjectType? _queryType;

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType)
    {
        // we are only interested in the query type to infer node resolvers
        // from the specified query fields.
        if ((completionContext.IsQueryType ?? false) &&
            definition is ObjectTypeDefinition typeDef &&
            completionContext.Type is ObjectType queryType)
        {
            var typeInspector = completionContext.TypeInspector;

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
                        completionContext.ReportError(
                            NodeResolver_MustHaveExactlyOneIdArg(
                                fieldDef.Name,
                                queryType));
                        continue;
                    }

                    var argument = fieldDef.Arguments[0];

                    if (argument.Type is null)
                    {
                        throw NodeResolver_ArgumentTypeMissing();
                    }

                    var fieldType = completionContext.GetType<IType>(fieldDef.Type);

                    if (!fieldType.IsObjectType())
                    {
                        completionContext.ReportError(
                            NodeResolver_MustReturnObject(
                                fieldDef.Name,
                                queryType));
                        continue;
                    }

                    var fieldTypeDef = ((ObjectType)fieldType.NamedType()).Definition;

                    if (fieldTypeDef is null)
                    {
                        throw NodeResolver_ObjNoDefinition();
                    }

                    var idDef = fieldTypeDef.Fields.FirstOrDefault(t => t.Name.EqualsOrdinal(Id));

                    if (idDef is null)
                    {
                        completionContext.ReportError(
                            NodeResolver_NodeTypeHasNoId(
                                (ObjectType)fieldType.NamedType()));
                        continue;
                    }

                    // we will ensure that the object type is implementing the node type interface.
                    fieldTypeDef.Interfaces.Add(typeInspector.GetTypeRef(typeof(NodeType)));
                    fieldTypeDef.ContextData[NodeResolver] = fieldDef.Name;

                    // We will ensure that the node id argument is always a non-null ID type.
                    argument.Type = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));
                    RelayIdFieldHelpers.AddSerializerToInputField(
                        completionContext,
                        argument,
                        fieldTypeDef.Name);

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
                var field = _queryType.Fields[fieldName];
                node[NodeResolver] = new NodeResolverInfo(field.Arguments[0], field.Middleware);
            }
        }
    }
}
