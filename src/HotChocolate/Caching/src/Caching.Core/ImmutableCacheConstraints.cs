using System.Collections.Immutable;

namespace HotChocolate.Caching;

/// <summary>
/// An immutable snapshot of the computed cache control constraints for an operation,
/// representing the most restrictive combination of all <c>@cacheControl</c> directives
/// encountered while walking the operation's selection set.
/// </summary>
internal sealed class ImmutableCacheConstraints(
    int? maxAge,
    int? sharedMaxAge,
    CacheControlScope scope,
    ImmutableArray<string> vary)
    : ICacheConstraints
{
    public int? MaxAge { get; } = maxAge;

    public int? SharedMaxAge { get; } = sharedMaxAge;

    public CacheControlScope Scope { get; } = scope;

    public ImmutableArray<string> Vary { get; } = vary;

    public string VaryString
    {
        get
        {
            field ??= Vary.Length is 0
                ? string.Empty
                : string.Join(", ", Vary);

            return field;
        }
    }
}
