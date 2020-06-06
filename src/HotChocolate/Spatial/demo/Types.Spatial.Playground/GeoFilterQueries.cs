using System.Collections.Generic;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Playground
{

    public class GeoFilterType : ObjectType<GeoFilterQueries>
    {
        protected override void Configure(IObjectTypeDescriptor<GeoFilterQueries> descriptor)
        {
            descriptor.Field(t => t.MapItems).UseFiltering<AreaOfInterest>();
        }
    }
    public class GeoFilterQueries
    {
        public IEnumerable<AreaOfInterest> MapItems { get; } = new[]
            {
                new AreaOfInterest { Shape = new Point(new Coordinate(1,1)) },
                new AreaOfInterest { Shape = new Point(new Coordinate(10,10)) },
                new AreaOfInterest { Shape = new Point(new Coordinate(100,100)) },
                new AreaOfInterest { Shape = new Point(new Coordinate(1000,1000)) },
                new AreaOfInterest { Shape = new Point(new Coordinate(10000,10000)) },
                new AreaOfInterest { Shape = new Point(new Coordinate(100000,100000)) },
            };
    }

    public class AreaOfInterest {
        public Point Shape { get; set; }
    }
}
