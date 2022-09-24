namespace HotChocolate.Caching;

/// <summary>
/// The scope of a query result.
/// </summary>
public enum CacheControlScope
{
    /// <summary>
    /// Private query results contain data
    /// that is scoped to a user and must therefore
    /// be only cached for and accessible to that user.
    /// </summary>
    Private,
    /// <summary>
    /// Public query results contain data
    /// that is not scoped to a particular user and can
    /// therefore be stored in a shared cache.
    /// </summary>
    Public,
}
