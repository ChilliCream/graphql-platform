using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public static partial class FederationSchemaPrinter
{
    private static IDefinitionNode? SerializeObjectType(
        ObjectType objectType,
        Context context)
    {
        var fields = objectType.Fields
            .Where(IncludeField)
            .Select(t => SerializeObjectField(t, context))
            .ToList();

        if (fields.Count == 0)
        {
            return null;
        }

        var directives = SerializeDirectives(objectType.Directives, context);

        var interfaces = objectType.Implements
            .Select(t => SerializeNamedType(t, context))
            .ToList();

        if (objectType.ContextData.ContainsKey(Constants.FederationContextData.ExtendMarker))
        {
            return new ObjectTypeExtensionNode(
                location: null,
                new NameNode(objectType.Name),
                directives.ReadOnlyList,
                interfaces,
                fields);
        }

        return new ObjectTypeDefinitionNode(
            location: null,
            new NameNode(objectType.Name),
            SerializeDescription(objectType.Description),
            directives.ReadOnlyList,
            interfaces,
            fields);
    }

    private static InterfaceTypeDefinitionNode SerializeInterfaceType(
        InterfaceType interfaceType,
        Context context)
    {
        var directives = SerializeDirectives(interfaceType.Directives, context);

        var fields = interfaceType.Fields
            .Select(t => SerializeObjectField(t, context))
            .ToList();

        return new InterfaceTypeDefinitionNode(
            location: null,
            new NameNode(interfaceType.Name),
            SerializeDescription(interfaceType.Description),
            directives.ReadOnlyList,
            Array.Empty<NamedTypeNode>(),
            fields);
    }

    private static UnionTypeDefinitionNode SerializeUnionType(
        UnionType unionType,
        Context context)
    {
        var directives = SerializeDirectives(unionType.Directives, context);

        var types = unionType.Types.Values
            .Select(t => SerializeNamedType(t, context))
            .ToList();

        return new UnionTypeDefinitionNode(
            location: null,
            new NameNode(unionType.Name),
            SerializeDescription(unionType.Description),
            directives.ReadOnlyList,
            types);
    }

    private static FieldDefinitionNode SerializeObjectField(
        IOutputField field,
        Context context)
    {
        var arguments = field.Arguments
            .Select(t => SerializeInputField(t, context))
            .ToList();

        var directives = SerializeDirectives(field.Directives, context);

        if (field.IsDeprecated)
        {
            var deprecateDirective = DeprecatedDirective.CreateNode(field.DeprecationReason);
            var temp = directives.GetOrCreateList();
            temp.Add(deprecateDirective);
        }

        return new FieldDefinitionNode(
            location: null,
            new NameNode(field.Name),
            SerializeDescription(field.Description),
            arguments,
            SerializeType(field.Type, context),
            directives.ReadOnlyList);
    }
}
