using HotChocolate.Types.Spatial.Configuration;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// Common extension for <see cref="ISpatialConventionDescriptor"/>
    /// </summary>
    public static class SpatialConventionDescriptorExtensions
    {
        /// <summary>
        /// Adds a coordinate system from a WellKnownText.
        /// </summary>
        /// <param name="descriptor">The convention descriptor</param>
        /// <param name="srid">The identifier of the coordinate system</param>
        /// <param name="wellKnownText">The definition in WellKnownText</param>
        /// <returns>The descriptor</returns>
        public static ISpatialConventionDescriptor AddCoordinateSystemFromString(
            this ISpatialConventionDescriptor descriptor,
            int srid,
            string wellKnownText)
        {
            if (CoordinateSystemWktReader.Parse(wellKnownText) is CoordinateSystem cs)
            {
                descriptor.AddCoordinateSystem(srid, cs);
            }

            return descriptor;
        }

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
