using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class MultiPolygonFilterType
        : GeometryFilterType<MultiPolygon>
    {
        protected override void Configure(IFilterInputTypeDescriptor<MultiPolygon> descriptor) {
            // Multipolygon Specific Filters
            base.Configure(descriptor);
        }
    }
}
