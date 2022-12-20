using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Language;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities.Introspection;

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
            .Select(l => new NameNode(l.MapDirectiveLocation().ToString()))
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
}