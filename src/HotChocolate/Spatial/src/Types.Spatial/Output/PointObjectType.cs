using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Output
{
    public class PointObjectType : ObjectType<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field(p => p.X)
                .Name("x")
                .Description("X or Longitude");

            descriptor.Field(p => p.Y)
                .Name("y")
                .Description("Y or Latitude");

            descriptor.Field(p => p.SRID)
                .Name("srid")
                .Description(
                    "Spatial Reference System Identifier. e.g. latitude/longitude (WGS84): 4326, web mercator: 3867");
        }
    }
}
