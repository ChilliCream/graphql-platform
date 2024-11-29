namespace HotChocolate.Types;

/// <summary>
/// Defines the resolver dependency injection scopes.
/// </summary>
public enum DependencyInjectionScope
{
    /// <summary>
    /// Use standard request service scope.
    /// </summary>
    Request,

    /// <summary>
    /// Use a separate service scope for the resolver.
    /// </summary>
    Resolver,
}
