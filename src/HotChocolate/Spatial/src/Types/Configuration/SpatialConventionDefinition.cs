using System.Collections.Generic;
using NetTopologySuite;
using ProjNet.CoordinateSystems;

namespace HotChocolate.Types.Spatial.Configuration
{
    public class SpatialConventionDefinition
    {
        public int DefaultSrid { get; set; } = NtsGeometryServices.Instance.DefaultSRID;

        public readonly Dictionary<int, CoordinateSystem> CoordinateSystems = new();
    }
}
