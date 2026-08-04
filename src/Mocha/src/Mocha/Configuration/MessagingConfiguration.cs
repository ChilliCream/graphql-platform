using Mocha.Features;

namespace Mocha;

/// <summary>
/// Base class for messaging configuration objects that support feature-based extensibility through
/// <see cref="IFeatureCollection"/>.
/// </summary>
public abstract class MessagingConfiguration : IFeatureProvider
{
    private IFeatureCollection? _features;

    /// <summary>
    /// Get access to context data that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    public virtual IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Get access to features that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    public IFeatureCollection GetFeatures() => _features ?? FeatureCollection.Empty;
}
