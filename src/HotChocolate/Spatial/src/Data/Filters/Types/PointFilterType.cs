using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class PointFilterType
        : FilterInputType<Point>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Point> descriptor)
        {
            // Point Specific Filters
            base.Configure(descriptor);
        }
    }
}
