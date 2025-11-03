using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Types.Mutable.Directives;

public sealed class CacheControlDirective(
    int? maxAge = null,
    CacheControlScope? scope = null,
    bool? inheritMaxAge = null,
    int? sharedMaxAge = null,
    ImmutableArray<string>? vary = null)
{
    public int? MaxAge { get; } = maxAge;

    public int? SharedMaxAge { get; } = sharedMaxAge;

    public bool? InheritMaxAge { get; } = inheritMaxAge;

    public CacheControlScope? Scope { get; } = scope;

    public ImmutableArray<string>? Vary { get; } = vary;

    public static CacheControlDirective From(IDirective directive)
    {
        var maxAge =
            ((IntValueNode?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.MaxAge))?.ToInt32();
        var scopeArg = ((EnumValueNode?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.Scope))?.Value;
        var inheritMaxAge =
            ((BooleanValueNode?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.InheritMaxAge))?.Value;
        var sharedMaxAge =
            ((IntValueNode?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.SharedMaxAge))?.ToInt32();
        var vary =
            ((ListValueNode?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.Vary))?
                .Items.OfType<StringValueNode>().Select(i => i.Value).ToImmutableArray();

        CacheControlScope? scope;
        if (Enum.TryParse(scopeArg, ignoreCase: true, out CacheControlScope scopeValue))
        {
            scope = scopeValue;
        }
        else
        {
            scope = null;
        }

        return new CacheControlDirective(
            maxAge,
            scope,
            inheritMaxAge,
            sharedMaxAge,
            vary);
    }
}

public enum CacheControlScope
{
    Private,
    Public
}
