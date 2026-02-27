using HotChocolate.Language;

namespace HotChocolate.Fusion;

/// <summary>
/// Compares two <see cref="DirectiveDefinitionNode"/> instances for structural equality,
/// ignoring descriptions and treating arguments and locations as unordered collections.
/// </summary>
internal sealed class DirectiveDefinitionNodeComparer
    : IEqualityComparer<DirectiveDefinitionNode>
{
    private static readonly IEqualityComparer<ISyntaxNode> s_syntaxComparer =
        SyntaxComparer.BySyntaxIgnoreDescriptions;

    public static DirectiveDefinitionNodeComparer Instance { get; } = new();

    public bool Equals(DirectiveDefinitionNode? x, DirectiveDefinitionNode? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Name.Value.Equals(y.Name.Value, StringComparison.Ordinal)
            && x.IsRepeatable == y.IsRepeatable
            && ArgumentsEqual(x.Arguments, y.Arguments)
            && LocationsEqual(x.Locations, y.Locations);
    }

    public int GetHashCode(DirectiveDefinitionNode obj)
    {
        var hashCode = new HashCode();

        hashCode.Add(obj.Name.Value, StringComparer.Ordinal);
        hashCode.Add(obj.IsRepeatable);

        // Use XOR for order-independent hashing of arguments.
        var argumentsHash = 0;
        foreach (var argument in obj.Arguments)
        {
            argumentsHash ^= StringComparer.Ordinal.GetHashCode(argument.Name.Value);
        }

        hashCode.Add(argumentsHash);

        // Use XOR for order-independent hashing of locations.
        var locationsHash = 0;
        foreach (var location in obj.Locations)
        {
            locationsHash ^= StringComparer.Ordinal.GetHashCode(location.Value);
        }

        hashCode.Add(locationsHash);

        return hashCode.ToHashCode();
    }

    private static bool ArgumentsEqual(
        IReadOnlyList<InputValueDefinitionNode> xArgs,
        IReadOnlyList<InputValueDefinitionNode> yArgs)
    {
        if (xArgs.Count != yArgs.Count)
        {
            return false;
        }

        if (xArgs.Count == 0)
        {
            return true;
        }

        // For each argument in x, find the matching argument in y by name
        // and compare them using the syntax comparer (ignoring descriptions).
        var matched = new bool[yArgs.Count];

        foreach (var xArg in xArgs)
        {
            var found = false;

            for (var j = 0; j < yArgs.Count; j++)
            {
                if (matched[j])
                {
                    continue;
                }

                var yArg = yArgs[j];

                if (xArg.Name.Value.Equals(yArg.Name.Value, StringComparison.Ordinal)
                    && s_syntaxComparer.Equals(xArg, yArg))
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

    private static bool LocationsEqual(
        IReadOnlyList<NameNode> xLocations,
        IReadOnlyList<NameNode> yLocations)
    {
        if (xLocations.Count != yLocations.Count)
        {
            return false;
        }

        if (xLocations.Count == 0)
        {
            return true;
        }

        // For each location in x, find the matching location in y by name.
        var matched = new bool[yLocations.Count];

        foreach (var xLocation in xLocations)
        {
            var found = false;

            for (var j = 0; j < yLocations.Count; j++)
            {
                if (matched[j])
                {
                    continue;
                }

                if (xLocation.Value.Equals(yLocations[j].Value, StringComparison.Ordinal))
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
}
