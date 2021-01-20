using HotChocolate.Types.Spatial.Configuration;
using ProjNet.CoordinateSystems;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// Common extension for <see cref="ISpatialConventionDescriptor"/>
    /// </summary>
    public static class SpatialConventionExtensions
    {
        /// <summary>
        /// Registers the coordinate system WGS84 (SRID: 4326) on the convention
        /// </summary>
        /// <param name="descriptor">The convention descriptor</param>
        /// <returns>The convention descriptor</returns>
        public static ISpatialConventionDescriptor AddWGS84(
            this ISpatialConventionDescriptor descriptor)
        {
            return descriptor.AddCoordinateSystem(4326, GeographicCoordinateSystem.WGS84);
        }

        /// <summary>
        /// Registers the coordinate system WebMercator (SRID: 3857) on the convention
        /// </summary>
        /// <param name="descriptor">The convention descriptor</param>
        /// <returns>The convention descriptor</returns>
        public static ISpatialConventionDescriptor AddWebMercator(
            this ISpatialConventionDescriptor descriptor)
        {
            return descriptor.AddCoordinateSystem(3857, ProjectedCoordinateSystem.WebMercator);
        }
    }
}
