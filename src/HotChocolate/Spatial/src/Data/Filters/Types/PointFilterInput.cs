using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public class PointFilterInput
        : GeometryFilterInput<Point>
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
