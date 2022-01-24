using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities.Introspection;

namespace HotChocolate.ApolloFederation;

internal static partial class FederationSchemaPrinter
{
    private static IDefinitionNode? SerializeObjectType(
        ObjectType objectType,
        ReferencedTypes referenced)
    {
        var fields = objectType.Fields
            .Where(IncludeField)
            .Select(t => SerializeObjectField(t, referenced))
            .ToList();

        if (fields.Count == 0)
        {
            return null;
        }

        var directives = objectType.Directives
            .Select(t => SerializeDirective(t, referenced))
            .ToList();

        var interfaces = objectType.Implements
            .Select(t => SerializeNamedType(t, referenced))
            .ToList();

        if (objectType.ContextData.ContainsKey(WellKnownContextData.ExtendMarker))
        {
            return new ObjectTypeExtensionNode(
                null,
                new NameNode(objectType.Name),
                directives,
                interfaces,
                fields);
        }

        return new ObjectTypeDefinitionNode(
            null,
            new NameNode(objectType.Name),
            SerializeDescription(objectType.Description),
            directives,
            interfaces,
            fields);
    }

    private static InterfaceTypeDefinitionNode SerializeInterfaceType(
        InterfaceType interfaceType,
        ReferencedTypes referenced)
    {
        var directives = interfaceType.Directives
            .Select(t => SerializeDirective(t, referenced))
            .ToList();

        var fields = interfaceType.Fields
            .Select(t => SerializeObjectField(t, referenced))
            .ToList();

        return new InterfaceTypeDefinitionNode(
            null,
            new NameNode(interfaceType.Name),
            SerializeDescription(interfaceType.Description),
            directives,
            Array.Empty<NamedTypeNode>(),
            fields);
    }

    private static UnionTypeDefinitionNode SerializeUnionType(
        UnionType unionType,
        ReferencedTypes referenced)
    {
        var directives = unionType.Directives
            .Select(t => SerializeDirective(t, referenced))
            .ToList();

        var types = unionType.Types.Values
            .Select(t => SerializeNamedType(t, referenced))
            .ToList();

        return new UnionTypeDefinitionNode(
            null,
            new NameNode(unionType.Name),
            SerializeDescription(unionType.Description),
            directives,
            types);
    }

    private static FieldDefinitionNode SerializeObjectField(
        IOutputField field,
        ReferencedTypes referenced)
    {
        var arguments = field.Arguments
            .Select(t => SerializeInputField(t, referenced))
            .ToList();

        var directives = field.Directives
            .Select(t => SerializeDirective(t, referenced))
            .ToList();

        return new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            SerializeDescription(field.Description),
            arguments,
            SerializeType(field.Type, referenced),
            directives);
    }
}
