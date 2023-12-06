using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using DirectiveLocationType = HotChocolate.Types.DirectiveLocation;

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

        var locations = DirectiveLocations(directiveType.Locations)
            .Select(l => new NameNode(DirectiveLocationExtensions.MapDirectiveLocation(l).ToString()))
            .ToList();

        return new DirectiveDefinitionNode
        (
            null,
            new NameNode(directiveType.Name),
            SerializeDescription(directiveType.Description),
            directiveType.IsRepeatable,
            arguments,
            locations
        );
    }

    private static IEnumerable<DirectiveLocationType> DirectiveLocations(DirectiveLocationType locations)
    {
        foreach (DirectiveLocationType value in Enum.GetValues(locations.GetType()))
        {
            if (locations.HasFlag(value))
            {
                yield return value;
            }
        }
    }
}
