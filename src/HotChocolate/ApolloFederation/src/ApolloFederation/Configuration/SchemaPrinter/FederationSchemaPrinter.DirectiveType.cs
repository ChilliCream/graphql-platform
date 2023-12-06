using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public static partial class FederationSchemaPrinter
{
    private static DirectiveDefinitionNode SerializeDirectiveTypeDefinition(
        DirectiveType directiveType,
        Context context)
    {
        var arguments = directiveType.Arguments
            .Select(a => SerializeInputField(a, context))
            .ToList();

        var locations = directiveType.Locations
            .AsEnumerable()
            .Select(l => new NameNode(l.MapDirectiveLocation().ToString()))
            .ToList();

        return new DirectiveDefinitionNode(
            location: null,
            new NameNode(directiveType.Name),
            SerializeDescription(directiveType.Description),
            directiveType.IsRepeatable,
            arguments,
            locations);
    }
}
