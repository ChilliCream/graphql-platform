using ProjNet.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration
{
    /// <summary>
    /// The descriptor of a <see cref="SpatialConvention"/>
    /// </summary>
    public interface ISpatialConventionDescriptor
    {
        /// <summary>
        /// The default SRID/CRS of the schema. All incoming queries will be translated to this SRID
        /// </summary>
        /// <param name="srid">The SRID that should be considered default</param>
        /// <returns>The descriptor</returns>
        ISpatialConventionDescriptor DefaultSrid(int srid);

        /// <summary>
        /// Adds a coordinate system from a WellKnownText.
        /// </summary>
        /// <param name="srid">The identifier of the coordinate system</param>
        /// <param name="wellKnownText">The definition in WellKnownText</param>
        /// <returns>The descriptor</returns>
        ISpatialConventionDescriptor AddCoordinateSystemFromString(int srid, string wellKnownText);

        /// <summary>
        /// Adds a coordinate system to the convention
        /// </summary>
        /// <param name="srid">The identifier of the coordinate system</param>
        /// <param name="coordinateSystem">The instance of the coordinate system</param>
        /// <returns>The descriptor</returns>
        ISpatialConventionDescriptor AddCoordinateSystem(
            int srid,
            CoordinateSystem coordinateSystem);
    }
}
