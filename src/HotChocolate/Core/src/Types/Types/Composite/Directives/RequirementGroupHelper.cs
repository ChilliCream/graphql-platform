using HotChocolate.Utilities;

namespace HotChocolate.Types.Composite;

internal static class RequirementGroupHelper
{
    internal static IReadOnlyList<IReadOnlyList<string>> Canonicalize(
        IReadOnlyList<IReadOnlyList<string>> groups,
        string directiveName,
        string paramName)
    {
        ArgumentNullException.ThrowIfNull(groups, paramName);

        var result = new List<IReadOnlyList<string>>();

        foreach (var group in groups)
        {
            var canonical = CreateGroup(group, directiveName, paramName);

            if (!result.Exists(existing => existing.SequenceEqual(canonical)))
            {
                result.Add(canonical);
            }
        }

        if (result.Count == 0)
        {
            throw ThrowHelper.AuthorizationDirective_GroupIsEmpty(directiveName, paramName);
        }

        result.Sort(CompareGroups);
        return result;
    }

    private static IReadOnlyList<string> CreateGroup(
        IEnumerable<string> values,
        string directiveName,
        string paramName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var set = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw ThrowHelper.AuthorizationDirective_ValueIsEmpty(directiveName, paramName);
            }

            set.Add(value);
        }

        if (set.Count == 0)
        {
            throw ThrowHelper.AuthorizationDirective_GroupIsEmpty(directiveName, paramName);
        }

        return [.. set];
    }

    private static int CompareGroups(IReadOnlyList<string> x, IReadOnlyList<string> y)
    {
        var length = Math.Min(x.Count, y.Count);

        for (var i = 0; i < length; i++)
        {
            var result = string.CompareOrdinal(x[i], y[i]);

            if (result != 0)
            {
                return result;
            }
        }

        return x.Count.CompareTo(y.Count);
    }
}
