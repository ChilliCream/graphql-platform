using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Rewrites <c>@key</c> directives on entity types to strip the <c>resolvable</c>
/// argument, keeping only the <c>fields</c> argument.
/// </summary>
internal static class RewriteKeyDirectives
{
    /// <summary>
    /// Applies the key directive rewrite to all type definitions in the document.
    /// </summary>
    /// <param name="document">
    /// The document to transform.
    /// </param>
    /// <returns>
    /// A new document with rewritten <c>@key</c> directives.
    /// </returns>
    public static DocumentNode Apply(DocumentNode document)
    {
        var definitions = new List<IDefinitionNode>(document.Definitions.Count);
        var changed = false;

        foreach (var definition in document.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode objectType:
                {
                    var rewritten = RewriteDirectivesOnObjectType(objectType);

                    if (!ReferenceEquals(rewritten, objectType))
                    {
                        changed = true;
                    }

                    definitions.Add(rewritten);
                    break;
                }

                case InterfaceTypeDefinitionNode interfaceType:
                {
                    var rewritten = RewriteDirectivesOnInterfaceType(interfaceType);

                    if (!ReferenceEquals(rewritten, interfaceType))
                    {
                        changed = true;
                    }

                    definitions.Add(rewritten);
                    break;
                }

                default:
                    definitions.Add(definition);
                    break;
            }
        }

        if (!changed)
        {
            return document;
        }

        return document.WithDefinitions(definitions);
    }

    private static ObjectTypeDefinitionNode RewriteDirectivesOnObjectType(
        ObjectTypeDefinitionNode objectType)
    {
        var rewritten = RewriteKeyDirectivesInList(objectType.Directives);

        if (rewritten is null)
        {
            return objectType;
        }

        return objectType.WithDirectives(rewritten);
    }

    private static InterfaceTypeDefinitionNode RewriteDirectivesOnInterfaceType(
        InterfaceTypeDefinitionNode interfaceType)
    {
        var rewritten = RewriteKeyDirectivesInList(interfaceType.Directives);

        if (rewritten is null)
        {
            return interfaceType;
        }

        return interfaceType.WithDirectives(rewritten);
    }

    private static List<DirectiveNode>? RewriteKeyDirectivesInList(
        IReadOnlyList<DirectiveNode> directives)
    {
        List<DirectiveNode>? result = null;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (!directive.Name.Value.Equals(
                    FederationDirectiveNames.Key,
                    StringComparison.Ordinal))
            {
                result?.Add(directive);
                continue;
            }

            // Check if we need to strip the resolvable argument.
            var hasResolvable = false;

            foreach (var argument in directive.Arguments)
            {
                if (argument.Name.Value.Equals("resolvable", StringComparison.Ordinal))
                {
                    hasResolvable = true;
                    break;
                }
            }

            if (!hasResolvable)
            {
                result?.Add(directive);
                continue;
            }

            // Lazily create the result list and copy previous items.
            if (result is null)
            {
                result = new List<DirectiveNode>(directives.Count);

                for (var j = 0; j < i; j++)
                {
                    result.Add(directives[j]);
                }
            }

            // Keep only the "fields" argument.
            var fieldsOnlyArgs = new List<ArgumentNode>();

            foreach (var argument in directive.Arguments)
            {
                if (argument.Name.Value.Equals("fields", StringComparison.Ordinal))
                {
                    fieldsOnlyArgs.Add(argument);
                }
            }

            result.Add(directive.WithArguments(fieldsOnlyArgs));
        }

        return result;
    }
}
