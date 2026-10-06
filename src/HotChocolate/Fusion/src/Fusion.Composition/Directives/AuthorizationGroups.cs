using System.Collections.Immutable;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Directives;

internal static class AuthorizationGroups
{
    public static MergedAuthorization Merge(IEnumerable<DirectivesProviderInfo> memberDefinitions)
    {
        var authenticated = false;
        var scopeFactors = new List<ImmutableArray<ImmutableArray<string>>>();
        var policyFactors = new List<ImmutableArray<ImmutableArray<string>>>();

        foreach (var (member, _) in memberDefinitions)
        {
            foreach (var directive in member.Directives)
            {
                switch (directive.Name)
                {
                    case DirectiveNames.Authenticated:
                        authenticated = true;
                        break;

                    case DirectiveNames.RequiresScopes:
                        scopeFactors.Add(
                            ParseGroups(directive, ArgumentNames.Scopes));
                        break;

                    case DirectiveNames.Policy:
                        policyFactors.Add(
                            ParseGroups(directive, ArgumentNames.Policies));
                        break;
                }
            }
        }

        return new MergedAuthorization(authenticated, Reduce(scopeFactors), Reduce(policyFactors));
    }

    /// <summary>
    /// Reads the requirement carried by a <c>@fusion__authorization</c> directive.
    /// </summary>
    public static MergedAuthorization FromFusionDirective(IDirective directive)
    {
        var authenticated = directive.Arguments.TryGetValue(ArgumentNames.Authenticated, out var value)
            && value is BooleanValueNode { Value: true };

        return new MergedAuthorization(
            authenticated,
            ReadGroups(directive, ArgumentNames.Scopes),
            ReadGroups(directive, ArgumentNames.Policies));
    }

    private static ImmutableArray<ImmutableArray<string>> ReadGroups(
        IDirective directive,
        string argumentName)
    {
        return directive.Arguments.ContainsName(argumentName)
            ? Reduce([ParseGroups(directive, argumentName)])
            : [];
    }

    /// <summary>
    /// Combines the alternative groups of every factor into the canonical reduced form: sorted,
    /// deduplicated, without supersets, ordered by length and then lexicographically.
    /// Every factor must contain at least one non-empty group.
    /// </summary>
    public static ImmutableArray<ImmutableArray<string>> Reduce(
        IEnumerable<ImmutableArray<ImmutableArray<string>>> factors)
    {
        var result = ImmutableArray<ImmutableArray<string>>.Empty;
        var hasFactor = false;

        foreach (var factor in factors)
        {
            var groups = factor.Select(Normalize).ToList();

            if (!hasFactor)
            {
                result = Minimize(groups);
                hasFactor = true;
                continue;
            }

            var combined = new List<ImmutableArray<string>>(result.Length * groups.Count);

            foreach (var left in result)
            {
                foreach (var right in groups)
                {
                    combined.Add(Normalize(left.AddRange(right)));
                }
            }

            result = Minimize(combined);
        }

        return result;
    }

    private static ImmutableArray<ImmutableArray<string>> ParseGroups(
        IDirective directive,
        string argumentName)
    {
        if (!directive.Arguments.TryGetValue(argumentName, out var argument))
        {
            throw ThrowHelper.AuthorizationDirectiveArgumentInvalid(directive.Name, argumentName);
        }

        // A single string coerces to a list of one group of one.
        if (argument is StringValueNode singleValue)
        {
            return [[singleValue.Value]];
        }

        if (argument is not ListValueNode outer)
        {
            throw ThrowHelper.AuthorizationDirectiveArgumentInvalid(directive.Name, argumentName);
        }

        var groups = ImmutableArray.CreateBuilder<ImmutableArray<string>>(outer.Items.Count);

        foreach (var item in outer.Items)
        {
            switch (item)
            {
                // A single string coerces to a group of one.
                case StringValueNode single:
                    groups.Add([single.Value]);
                    break;

                case ListValueNode inner:
                    var values = ImmutableArray.CreateBuilder<string>(inner.Items.Count);

                    foreach (var value in inner.Items)
                    {
                        if (value is not StringValueNode stringValue)
                        {
                            throw ThrowHelper.AuthorizationDirectiveArgumentInvalid(
                                directive.Name,
                                argumentName);
                        }

                        values.Add(stringValue.Value);
                    }

                    groups.Add(values.ToImmutable());
                    break;

                default:
                    throw ThrowHelper.AuthorizationDirectiveArgumentInvalid(
                        directive.Name,
                        argumentName);
            }
        }

        return groups.ToImmutable();
    }

    private static ImmutableArray<string> Normalize(ImmutableArray<string> group)
    {
        var set = new SortedSet<string>(group, StringComparer.Ordinal);
        return [.. set];
    }

    private static ImmutableArray<ImmutableArray<string>> Minimize(
        List<ImmutableArray<string>> groups)
    {
        groups.Sort(CompareGroups);

        var kept = new List<ImmutableArray<string>>(groups.Count);

        foreach (var candidate in groups)
        {
            var redundant = false;

            foreach (var existing in kept)
            {
                if (IsSubset(existing, candidate))
                {
                    redundant = true;
                    break;
                }
            }

            if (!redundant)
            {
                kept.Add(candidate);
            }
        }

        return [.. kept];
    }

    // Both groups are sorted and deduplicated.
    private static bool IsSubset(ImmutableArray<string> subset, ImmutableArray<string> superset)
    {
        if (subset.Length > superset.Length)
        {
            return false;
        }

        var j = 0;

        foreach (var value in subset)
        {
            while (j < superset.Length
                && string.CompareOrdinal(superset[j], value) < 0)
            {
                j++;
            }

            if (j == superset.Length || !string.Equals(superset[j], value, StringComparison.Ordinal))
            {
                return false;
            }

            j++;
        }

        return true;
    }

    private static int CompareGroups(ImmutableArray<string> x, ImmutableArray<string> y)
    {
        if (x.Length != y.Length)
        {
            return x.Length.CompareTo(y.Length);
        }

        for (var i = 0; i < x.Length; i++)
        {
            var result = string.CompareOrdinal(x[i], y[i]);

            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }
}
