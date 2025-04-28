using ProjNet.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration;

/// <summary>
/// A convention that configures the behavior of spatial types
/// </summary>
public class SpatialConventionDescriptor : ISpatialConventionDescriptor
{
    /// <summary>
    /// The definition of this descriptor
    /// </summary>
    protected SpatialConventionDefinition Configuration { get; } = new();

    /// <summary>
    /// Creates the definition of this descriptor
    /// </summary>
    /// <returns></returns>
    public SpatialConventionDefinition CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public ISpatialConventionDescriptor DefaultSrid(int srid)
    {
        Configuration.DefaultSrid = srid;
        return this;
    }

    /// <inheritdoc />
    public ISpatialConventionDescriptor AddCoordinateSystem(
        int srid,
        CoordinateSystem coordinateSystem)
    {
        Configuration.CoordinateSystems[srid] = coordinateSystem;
        return this;
    }

    /// <summary>
    /// Creates a new instance of the descriptor
    /// </summary>
    /// <returns>A new instance of the descriptor</returns>
    public static SpatialConventionDescriptor New() => new();
}
