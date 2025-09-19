using System.Collections.Immutable;

namespace HotChocolate.Caching;

/// <summary>
/// The `@cacheControl` directive may be provided for individual fields or
/// entire object, interface or union types to provide caching hints to
/// the executor.
/// </summary>
/// <param name="maxAge">
/// The maximum amount of time this field's cached value is valid,
/// in seconds.
/// </param>
/// <param name="scope">
/// If `PRIVATE`, the field's value is specific to a single user.
/// The default value is `PUBLIC`, which means the field's value
/// is not tied to a single user.
/// </param>
/// <param name="inheritMaxAge">
/// If `true`, the field inherits the `maxAge` of its parent field.
/// </param>
/// <param name="sharedMaxAge">
/// Gets the maximum amount of time this field's cached value is valid
/// in shared caches like CDNs, in seconds.
/// </param>
/// <param name="vary">
/// The Vary HTTP response header describes the parts
/// of the request message aside from the method and URL
/// that influenced the content of the response it occurs in.
/// Most often, this is used to create a cache key when content
/// negotiation is in use.
/// </param>
public sealed class CacheControlDirective(
    int? maxAge = null,
    CacheControlScope? scope = null,
    bool? inheritMaxAge = null,
    int? sharedMaxAge = null,
    ImmutableArray<string>? vary = null)
{
    /// <summary>
    /// Gets the maximum amount of time this field's cached value is valid,
    /// in seconds.
    /// </summary>
    public int? MaxAge { get; } = maxAge;

    /// <summary>
    /// Gets the maximum amount of time this field's cached value is valid
    /// in shared caches like CDNs, in seconds.
    /// </summary>
    public int? SharedMaxAge { get; } = sharedMaxAge;

    /// <summary>
    /// If `true`, the field inherits the `maxAge` of its parent field.
    /// </summary>
    public bool? InheritMaxAge { get; } = inheritMaxAge;

    /// <summary>
    /// If `PRIVATE`, the field's value is specific to a single user.
    /// The default value is `PUBLIC`, which means the field's value
    /// is not tied to a single user.
    /// </summary>
    public CacheControlScope? Scope { get; } = scope;

    /// <summary>
    /// <para>
    /// Gets the vary HTTP response headers.
    /// </para>
    /// <para>
    /// The Vary HTTP response header describes the parts
    /// of the request message aside from the method and URL
    /// that influenced the content of the response it occurs in.
    /// Most often, this is used to create a cache key when content
    /// negotiation is in use.
    /// </para>
    /// </summary>
    public ImmutableArray<string>? Vary { get; } = vary;
}
