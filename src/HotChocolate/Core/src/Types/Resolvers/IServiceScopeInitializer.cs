namespace HotChocolate.Resolvers;

/// <summary>
/// Implement this to copy state from the requestScope to the resolver scope.
/// </summary>
public interface IServiceScopeInitializer
{
    /// <summary>
    /// Initializes the resolver scope with state from the request scope.
    /// </summary>
    /// <param name="requestScope">
    /// The request scope from which state shall be copied.
    /// </param>
    /// <param name="resolverScope">
    /// The resolver scope to which state shall be copied.
    /// </param>
    void Initialize(IServiceProvider requestScope, IServiceProvider resolverScope);
}
