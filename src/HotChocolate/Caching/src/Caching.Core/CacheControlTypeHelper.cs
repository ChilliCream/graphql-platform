using HotChocolate.Types;

namespace HotChocolate.Caching;

/// <summary>
/// Resolves which type's <c>@cacheControl</c> settings apply to a field.
/// </summary>
internal static class CacheControlTypeHelper
{
    /// <summary>
    /// Gets the type whose <c>@cacheControl</c> applies to a field of the given type,
    /// or <c>null</c> when no type settings apply to the field.
    /// </summary>
    public static IDirectivesProvider? GetCacheControlType(IType fieldType)
        => fieldType as IDirectivesProvider;
}
