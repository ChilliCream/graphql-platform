namespace HotChocolate;

/// <summary>
/// Allows access to the root service provider.
/// This allows schema services to access application level services.
/// </summary>
// TODO: Remove this
public interface IRootServiceProviderAccessor
{
    /// <summary>
    /// Gets the root service provider.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
