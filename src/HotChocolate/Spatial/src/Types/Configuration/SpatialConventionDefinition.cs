using System.Collections.Generic;
using NetTopologySuite;
using ProjNet.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration
{
    /// <summary>
    /// The definition of the spatial convention
    /// </summary>
    public class SpatialConventionDefinition
    {
        /// <summary>
        /// The default SRID/CRS of the schema. All incoming queries will be translated to this SRID
        /// </summary>
        public int DefaultSrid { get; set; } = NtsGeometryServices.Instance.DefaultSRID;

        /// <summary>
        /// Stores a lookup of SRID and their coordinates systems
        /// </summary>
        public Dictionary<int, CoordinateSystem> CoordinateSystems { get; } = new();
    }
}
