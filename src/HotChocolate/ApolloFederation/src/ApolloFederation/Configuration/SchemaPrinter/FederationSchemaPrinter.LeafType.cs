using System.Linq;
using HotChocolate.Language;
using static HotChocolate.Types.SpecifiedByDirectiveType.Names;

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
            location: null,
            new NameNode(enumType.Name),
            SerializeDescription(enumType.Description),
            directives.ReadOnlyList,
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
            var temp = directives.GetOrCreateList();
            temp.Add(deprecateDirective);
        }

        return new EnumValueDefinitionNode(
            location: null,
            new NameNode(enumValue.Name),
            SerializeDescription(enumValue.Description),
            directives.ReadOnlyList);
    }

    private static ScalarTypeDefinitionNode SerializeScalarType(
        ScalarType scalarType,
        Context context)
    {
        var directives = SerializeDirectives(scalarType.Directives, context);

        if (scalarType.SpecifiedBy is not null)
        {
            directives.GetOrCreateList().Add(
                new DirectiveNode(
                    SpecifiedBy,
                    new ArgumentNode(
                        Url,
                        new StringValueNode(scalarType.SpecifiedBy.ToString()))));
        }

        return new(
            location: null,
            new NameNode(scalarType.Name),
            SerializeDescription(scalarType.Description),
            directives.ReadOnlyList);
    }
}
