using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Transformation;
using NetTopologySuite;

namespace HotChocolate.Types.Spatial.Configuration;

/// <summary>
/// A convention that configures the behavior of spatial types
/// </summary>
public class SpatialConvention
    : Convention<SpatialConventionConfiguration>
    , ISpatialConvention
{
    private Action<ISpatialConventionDescriptor>? _configure;

    /// <inheritdoc />
    public SpatialConvention()
    {
        _configure = Configure;
    }

    /// <inheritdoc />
    public int DefaultSrid { get; private set; } = NtsGeometryServices.Instance.DefaultSRID;

    /// <inheritdoc />
    public IGeometryTransformerFactory TransformerFactory { get; private set; } = null!;

    /// <inheritdoc />
    public SpatialConvention(Action<ISpatialConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    /// <inheritdoc />
    protected override SpatialConventionConfiguration CreateConfiguration(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException();
        }

        var descriptor = SpatialConventionDescriptor.New();

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    /// <summary>
    /// This method is called on initialization of the convention but before the convention is
    /// completed. The default implementation of this method does nothing. It can be overridden
    /// by a derived class such that the convention can be further configured before it is
    /// completed
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor that can be used to configure the convention
    /// </param>
    protected virtual void Configure(ISpatialConventionDescriptor descriptor)
    {
    }

    /// <inheritdoc />
    protected override void Complete(IConventionContext context)
    {
        if (Configuration is null)
        {
            throw new InvalidOperationException();
        }

        DefaultSrid = Configuration.DefaultSrid;
        TransformerFactory = new GeometryTransformerFactory(Configuration.CoordinateSystems);
    }
}
