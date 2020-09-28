using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Types.Spatial;
using static HotChocolate.Data.Spatial.Filters.SpatialFilterOperations;

namespace HotChocolate.Data.Spatial.Filters
{
    public class GeometryOverlapsOperationType : BooleanOperationFilterInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Geometry).Type<NonNullType<GeometryType>>();
            descriptor.Operation(Buffer).Type<FloatType>();

            base.Configure(descriptor);
        }
    }
}
