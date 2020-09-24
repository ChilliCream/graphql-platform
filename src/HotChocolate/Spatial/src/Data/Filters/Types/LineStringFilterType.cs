using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class LineStringFilterType
        : GeometryFilterType<LineString>
    {
        protected override void Configure(IFilterInputTypeDescriptor<LineString> descriptor)
        {
            // Line String Specific Filters
            base.Configure(descriptor);
        }
    }
}
