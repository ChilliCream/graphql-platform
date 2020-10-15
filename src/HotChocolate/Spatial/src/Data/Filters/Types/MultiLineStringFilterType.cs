using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class MultiLineStringFilterType
        : GeometryFilterType<MultiLineString>
    {
        protected override void Configure(IFilterInputTypeDescriptor<MultiLineString> descriptor)
        {
            // MultiLine String Specific Filters
            base.Configure(descriptor);
        }
    }
}
