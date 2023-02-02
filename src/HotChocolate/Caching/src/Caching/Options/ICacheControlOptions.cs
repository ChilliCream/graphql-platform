namespace HotChocolate.Caching;

/// <summary>
/// The options for caching query results.
/// </summary>
public interface ICacheControlOptions
{
    /// <summary>
    /// Denotes whether query result caching is enabled or not.
    /// </summary>
    bool Enable { get; }

    /// <summary>
    /// The <c>MaxAge</c> that should be applied to fields,
    /// if <see cref="ApplyDefaults"/> is <c>true</c>.
    /// Defaults to <c>0</c>.
    /// </summary>
    int DefaultMaxAge { get; }

    /// <summary>
    /// Denotes whether the <see cref="DefaultMaxAge"/> should be applied to all
    /// fields that do not already specify a <see cref="CacheControlDirective"/>,
    /// are fields on the Query root type or are responsible for fetching data.
    /// </summary>
    bool ApplyDefaults { get; }
}
