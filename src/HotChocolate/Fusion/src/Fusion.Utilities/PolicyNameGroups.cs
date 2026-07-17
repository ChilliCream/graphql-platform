using System.Collections.Immutable;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

/// <summary>
/// Provides the shared coercion, canonicalization, key creation, and formatting logic
/// for policy name groups. A policy expression is a disjunction of policy name groups:
/// names within a group combine with AND, groups combine with OR. The composition side
/// (@policy) and the execution side (@fusion__policy) both build on this class so that
/// the canonical wire form cannot drift between the two assemblies.
/// </summary>
internal static class PolicyNameGroups
{
    private const char EscapeCharacter = '\u001b';
    private const char NameSeparator = '\u001f';
    private const char GroupSeparator = '\u001e';

    /// <summary>
    /// Coerces the value of a `names` argument into policy name groups.
    /// A string coerces to a single group with a single name, a list item that
    /// is a string coerces to a group with a single name, and a list item that
    /// is a list of strings forms one group.
    /// </summary>
    /// <param name="value">The `names` argument value.</param>
    /// <param name="directiveName">
    /// The annotated directive name used in error messages, for example @policy.
    /// </param>
    public static ImmutableArray<ImmutableArray<string>> ParseNames(
        IValueNode value,
        string directiveName)
    {
        switch (value)
        {
            case StringValueNode stringValueNode:
                return [[stringValueNode.Value]];

            case ListValueNode listValueNode:
                if (listValueNode.Items.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"The `names` argument on {directiveName} must contain at least "
                        + "one policy name group.");
                }

                var groups = ImmutableArray.CreateBuilder<ImmutableArray<string>>(
                    listValueNode.Items.Count);

                foreach (var item in listValueNode.Items)
                {
                    groups.Add(ParseGroup(item, directiveName));
                }

                return groups.MoveToImmutable();

            default:
                throw new InvalidOperationException(
                    $"The `names` argument on {directiveName} must be a string or a list "
                    + "of policy name groups.");
        }
    }

    private static ImmutableArray<string> ParseGroup(IValueNode value, string directiveName)
    {
        switch (value)
        {
            case StringValueNode stringValueNode:
                return [stringValueNode.Value];

            case ListValueNode listValueNode:
                if (listValueNode.Items.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"A policy name group on {directiveName} must contain "
                        + "at least one policy name.");
                }

                var names = ImmutableArray.CreateBuilder<string>(listValueNode.Items.Count);

                foreach (var item in listValueNode.Items)
                {
                    if (item is not StringValueNode nameNode)
                    {
                        throw new InvalidOperationException(
                            $"A policy name on {directiveName} must be a string.");
                    }

                    names.Add(nameNode.Value);
                }

                return names.MoveToImmutable();

            default:
                throw new InvalidOperationException(
                    $"A policy name group on {directiveName} must be a string or "
                    + "a list of strings.");
        }
    }

    /// <summary>
    /// Canonicalizes policy name groups: names within a group are deduplicated and
    /// ordered, duplicate groups are removed, and groups are ordered deterministically.
    /// </summary>
    public static ImmutableArray<ImmutableArray<string>> Canonicalize(
        ImmutableArray<ImmutableArray<string>> groups)
    {
        var canonicalGroups = new List<ImmutableArray<string>>();
        var groupKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var names = group
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToImmutableArray();

            if (groupKeys.Add(CreateGroupKey(names)))
            {
                canonicalGroups.Add(names);
            }
        }

        canonicalGroups.Sort(
            static (left, right) => string.CompareOrdinal(
                CreateGroupKey(left),
                CreateGroupKey(right)));

        return [.. canonicalGroups];
    }

    /// <summary>
    /// Creates a canonical string key that identifies a policy expression independent
    /// of name or group order. Two expressions produce the same key only when their
    /// canonical groups are identical.
    /// </summary>
    public static string CreateCanonicalKey(ImmutableArray<ImmutableArray<string>> groups)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < groups.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(GroupSeparator);
            }

            AppendGroupKey(builder, groups[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a policy expression for diagnostics, for example (a AND b) OR c.
    /// </summary>
    public static string Format(ImmutableArray<ImmutableArray<string>> groups)
    {
        if (groups.Length == 1)
        {
            return string.Join(" AND ", groups[0]);
        }

        var builder = new StringBuilder();

        foreach (var group in groups)
        {
            if (builder.Length > 0)
            {
                builder.Append(" OR ");
            }

            if (group.Length == 1)
            {
                builder.Append(group[0]);
            }
            else
            {
                builder.Append('(');
                builder.AppendJoin(" AND ", group);
                builder.Append(')');
            }
        }

        return builder.ToString();
    }

    private static string CreateGroupKey(ImmutableArray<string> names)
    {
        var builder = new StringBuilder();
        AppendGroupKey(builder, names);
        return builder.ToString();
    }

    private static void AppendGroupKey(StringBuilder builder, ImmutableArray<string> names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(NameSeparator);
            }

            // Separator characters that occur within a name are escaped so that a name
            // containing a separator cannot produce the same key as a different expression.
            foreach (var c in names[i])
            {
                if (c is EscapeCharacter or NameSeparator or GroupSeparator)
                {
                    builder.Append(EscapeCharacter);
                }

                builder.Append(c);
            }
        }
    }
}
