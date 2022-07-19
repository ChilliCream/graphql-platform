using HotChocolate.Types;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Data.Filters.Spatial;

public class GeometryTouchesOperationFilterInputType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(SpatialFilterOperations.Geometry).Type<NonNullType<GeometryType>>();
        descriptor.Operation(SpatialFilterOperations.Buffer).Type<FloatType>();
        descriptor.AllowAnd(false).AllowOr(false);
    }
}
