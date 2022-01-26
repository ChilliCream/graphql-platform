using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

internal static partial class FederationSchemaPrinter
{
    private static InputObjectTypeDefinitionNode SerializeInputObjectType(
        InputObjectType inputObjectType,
        ReferencedTypes referenced)
    {
        var directives = inputObjectType.Directives
            .Select(t => SerializeDirective(t, referenced))
            .ToList();

        var fields = inputObjectType.Fields
            .Select(t => SerializeInputField(t, referenced))
            .ToList();

        return new InputObjectTypeDefinitionNode(
            null,
            new NameNode(inputObjectType.Name),
            SerializeDescription(inputObjectType.Description),
            directives,
            fields);
    }

    private static InputValueDefinitionNode SerializeInputField(
        IInputField inputValue,
        ReferencedTypes referenced)
    {
        return new InputValueDefinitionNode(
            null,
            new NameNode(inputValue.Name),
            SerializeDescription(inputValue.Description),
            SerializeType(inputValue.Type, referenced),
            inputValue.DefaultValue,
            inputValue.Directives.Select(t => SerializeDirective(t, referenced)).ToList());
    }
}
