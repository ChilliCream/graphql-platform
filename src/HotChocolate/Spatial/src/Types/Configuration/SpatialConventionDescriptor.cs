using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration
{
    /// <summary>
    /// A conventions that configures the behaviour of spatial types
    /// </summary>
    public class SpatialConventionDescriptor : ISpatialConventionDescriptor
    {
        /// <summary>
        /// The definition of this descriptor
        /// </summary>
        protected SpatialConventionDefinition Definition { get; } = new();

        /// <summary>
        /// Creates the definition of this descriptor
        /// </summary>
        /// <returns></returns>
        public SpatialConventionDefinition CreateDefinition()
        {
            return Definition;
        }

        /// <inheritdoc />
        public ISpatialConventionDescriptor DefaultSrid(int srid)
        {
            Definition.DefaultSrid = srid;
            return this;
        }

        /// <inheritdoc />
        public ISpatialConventionDescriptor AddCoordinateSystemFromString(
            int srid,
            string wellKnownText)
        {
            if (CoordinateSystemWktReader.Parse(wellKnownText) is CoordinateSystem cs)
            {
                Definition.CoordinateSystems[srid] = cs;
            }

            return this;
        }

        /// <summary>
        /// Creates a new instance of the descriptor
        /// </summary>
        /// <returns>A new instance of the descriptor</returns>
        public static SpatialConventionDescriptor New() => new();
    }
}
