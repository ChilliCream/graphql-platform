using HotChocolate.Language;

namespace HotChocolate.Fusion;

/// <summary>
/// Determines whether a source schema's <see cref="DirectiveDefinitionNode"/> is compatible
/// with a canonical directive definition, ignoring descriptions and treating arguments as an
/// unordered collection. A source definition is compatible when its name and repeatability
/// match the canonical definition exactly, its arguments match the canonical definition's
/// arguments exactly (ignoring order), and every location it declares is also declared by the
/// canonical definition. The canonical definition may declare additional locations that the
/// source definition does not.
/// </summary>
internal static class DirectiveDefinitionCompatibility
{
    private static readonly IEqualityComparer<ISyntaxNode> s_syntaxComparer =
        SyntaxComparer.BySyntaxIgnoreDescriptions;

    /// <summary>
    /// Determines whether <paramref name="source"/> is compatible with
    /// <paramref name="canonical"/>.
    /// </summary>
    /// <param name="source">The source schema's directive definition.</param>
    /// <param name="canonical">The canonical directive definition to compare against.</param>
    /// <param name="allowArgumentSubset">
    /// When <see langword="true"/>, the source definition's arguments only need to be a subset
    /// of the canonical definition's arguments (same name, same type; default values and
    /// descriptions are never compared) instead of matching them exactly.
    /// </param>
    public static bool IsSourceCompatibleWithCanonical(
        DirectiveDefinitionNode source,
        DirectiveDefinitionNode canonical,
        bool allowArgumentSubset = false)
    {
        if (ReferenceEquals(source, canonical))
        {
            return true;
        }

        return source.Name.Value.Equals(canonical.Name.Value, StringComparison.Ordinal)
            && source.IsRepeatable == canonical.IsRepeatable
            && (allowArgumentSubset
                ? ArgumentsSubsetOfCanonical(source.Arguments, canonical.Arguments)
                : ArgumentsEqual(source.Arguments, canonical.Arguments))
            && LocationsSubsetOfCanonical(source.Locations, canonical.Locations);
    }

    private static bool ArgumentsEqual(
        IReadOnlyList<InputValueDefinitionNode> sourceArgs,
        IReadOnlyList<InputValueDefinitionNode> canonicalArgs)
    {
        if (sourceArgs.Count != canonicalArgs.Count)
        {
            return false;
        }

        if (sourceArgs.Count == 0)
        {
            return true;
        }

        // For each source argument, find the matching canonical argument by name
        // and compare them using the syntax comparer (ignoring descriptions).
        var matched = new bool[canonicalArgs.Count];

        foreach (var sourceArg in sourceArgs)
        {
            var found = false;

            for (var j = 0; j < canonicalArgs.Count; j++)
            {
                if (matched[j])
                {
                    continue;
                }

                var canonicalArg = canonicalArgs[j];

                if (sourceArg.Name.Value.Equals(canonicalArg.Name.Value, StringComparison.Ordinal)
                    && s_syntaxComparer.Equals(sourceArg, canonicalArg))
                {
                    matched[j] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A source definition's arguments are compatible when every source argument's name is
    /// also declared by the canonical definition with the same type. Default values and
    /// descriptions are never compared, and the canonical definition may declare additional
    /// arguments the source does not.
    /// </summary>
    private static bool ArgumentsSubsetOfCanonical(
        IReadOnlyList<InputValueDefinitionNode> sourceArgs,
        IReadOnlyList<InputValueDefinitionNode> canonicalArgs)
    {
        foreach (var sourceArg in sourceArgs)
        {
            var found = false;

            foreach (var canonicalArg in canonicalArgs)
            {
                if (sourceArg.Name.Value.Equals(canonicalArg.Name.Value, StringComparison.Ordinal)
                    && s_syntaxComparer.Equals(sourceArg.Type, canonicalArg.Type))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool LocationsSubsetOfCanonical(
        IReadOnlyList<NameNode> sourceLocations,
        IReadOnlyList<NameNode> canonicalLocations)
    {
        // Every location declared by the source must also be declared by the canonical
        // definition. The canonical definition may declare locations the source does not.
        foreach (var sourceLocation in sourceLocations)
        {
            var found = false;

            foreach (var canonicalLocation in canonicalLocations)
            {
                if (sourceLocation.Value.Equals(canonicalLocation.Value, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
