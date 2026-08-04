namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Implement this interface to provide access to a fusion configuration storage.
/// </summary>
public interface IFusionConfigurationProvider
    : IObservable<FusionConfiguration>
    , IAsyncDisposable
{
    /// <summary>
    /// Gets the latest available version of the fusion configuration.
    /// </summary>
    FusionConfiguration? Configuration { get; }
}
