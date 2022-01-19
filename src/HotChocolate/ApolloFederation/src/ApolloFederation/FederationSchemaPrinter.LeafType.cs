using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation;

internal static partial class FederationSchemaPrinter
{
    private static EnumTypeDefinitionNode SerializeEnumType(
        EnumType enumType,
        ReferencedTypes referenced)
    {
        var directives = enumType.Directives
            .Select(
                t => SerializeDirective(
                    t,
                    referenced))
            .ToList();

        var values = enumType.Values
            .Select(
                t => SerializeEnumValue(
                    t,
                    referenced))
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
        ReferencedTypes referenced)
    {
        var directives = enumValue.Directives
            .Select(
                t => SerializeDirective(
                    t,
                    referenced))
            .ToList();

        return new EnumValueDefinitionNode(
            null,
            new NameNode(enumValue.Name),
            SerializeDescription(enumValue.Description),
            directives
        );
    }

    private static ScalarTypeDefinitionNode SerializeScalarType(
        ScalarType scalarType,
        ReferencedTypes referenced)
    {
        var directives = scalarType.Directives
            .Select(d => SerializeDirective(d, referenced))
            .ToList();

        return new(
            null,
            new NameNode(scalarType.Name),
            SerializeDescription(scalarType.Description),
            directives);
    }
}
