using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration
{
    public class SpatialConventionDescriptor : ISpatialConventionDescriptor
    {
        protected SpatialConventionDefinition Definition { get; } = new();

        public SpatialConventionDefinition CreateDefinition()
        {
            return Definition;
        }

        public ISpatialConventionDescriptor DefaultSrid(int srid)
        {
            Definition.DefaultSrid = srid;
            return this;
        }

        public ISpatialConventionDescriptor AddCoordinateSystemFromString(int srid, string wellKnownText)
        {
            if (CoordinateSystemWktReader.Parse(wellKnownText) is CoordinateSystem cs)
            {
                Definition.CoordinateSystems[srid] = cs;
            }
            return this;
        }

        public static SpatialConventionDescriptor New() => new();
    }
}
