using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Directives;

/// <summary>
/// Represents one @policy application. The policy expression is a disjunction of
/// policy name groups: names within a group combine with AND, groups combine with OR.
/// </summary>
internal sealed class PolicyDirective
{
    private PolicyDirective(
        ImmutableArray<ImmutableArray<string>> groups,
        string onDenied,
        string canonicalKey)
    {
        Groups = groups;
        OnDenied = onDenied;
        CanonicalKey = canonicalKey;
    }

    /// <summary>
    /// Gets the canonicalized policy name groups.
    /// </summary>
    public ImmutableArray<ImmutableArray<string>> Groups { get; }

    /// <summary>
    /// Gets the behavior used when this policy expression denies an entity.
    /// </summary>
    public string OnDenied { get; }

    /// <summary>
    /// Gets a canonical string key that identifies the policy expression
    /// independent of name or group order.
    /// </summary>
    public string CanonicalKey { get; }

    public static PolicyDirective Create(
        ImmutableArray<ImmutableArray<string>> groups,
        string onDenied)
    {
        var canonicalGroups = PolicyNameGroups.Canonicalize(groups);
        return new PolicyDirective(
            canonicalGroups,
            onDenied,
            PolicyNameGroups.CreateCanonicalKey(canonicalGroups));
    }

    public static PolicyDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(ArgumentNames.Names, out var namesArg))
        {
            throw new InvalidOperationException(
                "The `names` argument is required on the @policy directive.");
        }

        var groups = PolicyNameGroups.ParseNames(namesArg, "@policy");
        var onDenied = "NULL";

        if (directive.Arguments.TryGetValue(ArgumentNames.OnDenied, out var onDeniedArg))
        {
            onDenied = onDeniedArg switch
            {
                EnumValueNode enumValueNode => GetOnDenied(enumValueNode.Value),
                _ => throw new InvalidOperationException(
                    "The `onDenied` argument on @policy must be an enum value.")
            };
        }

        return Create(groups, onDenied);
    }

    private static string GetOnDenied(string onDeniedValue)
    {
        return onDeniedValue switch
        {
            "NULL" => "NULL",
            "ERROR" => "ERROR",
            "ABORT" => "ABORT",
            _ => throw new InvalidOperationException(
                $"The value `{onDeniedValue}` is not supported by @policy onDenied.")
        };
    }
}
