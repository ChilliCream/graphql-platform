using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Composite;

internal static class AuthorizationGroups
{
    public static IReadOnlyList<string> CreateGroup(
        string directiveName,
        IEnumerable<string> values,
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

    public static IReadOnlyList<IReadOnlyList<string>> Canonicalize(
        string directiveName,
        IEnumerable<IEnumerable<string>> groups,
        string paramName)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var result = new List<IReadOnlyList<string>>();

        foreach (var group in groups)
        {
            var canonical = CreateGroup(directiveName, group, paramName);

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

    public static void AddGroup<TDirective>(
        IDirectiveConfigurationProvider configuration,
        ITypeInspector typeInspector,
        string directiveName,
        string[] values,
        string paramName,
        Func<TDirective, IReadOnlyList<IReadOnlyList<string>>> getGroups,
        Func<IReadOnlyList<IReadOnlyList<string>>, TDirective> create)
        where TDirective : class
    {
        ArgumentNullException.ThrowIfNull(values);

        var group = CreateGroup(directiveName, values, paramName);
        var groups = new List<IReadOnlyList<string>>();

        for (var i = configuration.Directives.Count - 1; i >= 0; i--)
        {
            if (configuration.Directives[i].Value is TDirective existing)
            {
                groups.AddRange(getGroups(existing));
                configuration.Directives.RemoveAt(i);
            }
        }

        groups.Add(group);
        configuration.AddDirective(create(groups), typeInspector);
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
