using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public static partial class FederationSchemaPrinter
{
    private static EnumTypeDefinitionNode SerializeEnumType(
        EnumType enumType,
        Context context)
    {
        var directives = SerializeDirectives(enumType.Directives, context);

        var values = enumType.Values
            .Select(t => SerializeEnumValue(t, context))
            .ToList();

        return new EnumTypeDefinitionNode(
            null,
            new NameNode(enumType.Name),
            SerializeDescription(enumType.Description),
            directives,
            values);
    }

    private static EnumValueDefinitionNode SerializeEnumValue(
        IEnumValue enumValue,
        Context context)
    {
        var directives = SerializeDirectives(enumValue.Directives, context);

        if (enumValue.IsDeprecated)
        {
            var deprecateDirective = DeprecatedDirective.CreateNode(enumValue.DeprecationReason);

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

        return new EnumValueDefinitionNode(
            null,
            new NameNode(enumValue.Name),
            SerializeDescription(enumValue.Description),
            directives);
    }

    private static ScalarTypeDefinitionNode SerializeScalarType(
        ScalarType scalarType,
        Context context)
    {
        var directives = SerializeDirectives(scalarType.Directives, context);

        return new(
            null,
            new NameNode(scalarType.Name),
            SerializeDescription(scalarType.Description),
            directives);
    }
}
