using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class PointFilterType
        : GeometryFilterType<Point>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Point> descriptor)
        {
            descriptor.Field(x => x.M);
            descriptor.Field(x => x.X);
            descriptor.Field(x => x.Y);
            descriptor.Field(x => x.Z);

            base.Configure(descriptor);
        }
    }
}
