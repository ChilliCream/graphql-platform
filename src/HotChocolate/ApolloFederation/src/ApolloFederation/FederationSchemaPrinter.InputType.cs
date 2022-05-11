using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public static partial class FederationSchemaPrinter
{
    private static InputObjectTypeDefinitionNode SerializeInputObjectType(
        InputObjectType inputObjectType,
        Context context)
    {
        var directives = SerializeDirectives(inputObjectType.Directives, context);

        var fields = inputObjectType.Fields
            .Select(t => SerializeInputField(t, context))
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
        Context context)
    {
        var directives = SerializeDirectives(inputValue.Directives, context);

        return new InputValueDefinitionNode(
            null,
            new NameNode(inputValue.Name),
            SerializeDescription(inputValue.Description),
            SerializeType(inputValue.Type, context),
            inputValue.DefaultValue,
            directives);
    }
}
