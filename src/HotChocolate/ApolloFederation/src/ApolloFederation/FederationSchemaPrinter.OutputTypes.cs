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

        if (objectType.ContextData.ContainsKey(Constants.WellKnownContextData.ExtendMarker))
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
        Context context)
    {
        var directives = SerializeDirectives(interfaceType.Directives, context);

        var fields = interfaceType.Fields
            .Select(t => SerializeObjectField(t, context))
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
        Context context)
    {
        var directives = SerializeDirectives(unionType.Directives, context);

        var types = unionType.Types.Values
            .Select(t => SerializeNamedType(t, context))
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
        Context context)
    {
        var arguments = field.Arguments
            .Select(t => SerializeInputField(t, context))
            .ToList();

        var directives = SerializeDirectives(field.Directives, context);

        if (field.IsDeprecated)
        {
            var deprecateDirective = DeprecatedDirective.CreateNode(field.DeprecationReason);

            if(directives.Count == 0)
            {
                directives = new[] { deprecateDirective };
            }
            else
            {
                var temp = directives.ToList();
                temp.Add(deprecateDirective);
                directives = temp;
            }
        }

        return new FieldDefinitionNode(
            null,
            new NameNode(field.Name),
            SerializeDescription(field.Description),
            arguments,
            SerializeType(field.Type, context),
            directives);
    }
}
