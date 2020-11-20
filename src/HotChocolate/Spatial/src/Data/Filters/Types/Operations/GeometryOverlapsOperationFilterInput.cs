using HotChocolate.Types;
using HotChocolate.Types.Spatial;
using static HotChocolate.Data.Filters.Spatial.SpatialFilterOperations;

namespace HotChocolate.Data.Filters.Spatial
{
    public class GeometryOverlapsOperationFilterInput : BooleanOperationFilterInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Geometry).Type<NonNullType<GeometryType>>();
            descriptor.Operation(Buffer).Type<FloatType>();
            base.Configure(descriptor);
        }
    }
}
