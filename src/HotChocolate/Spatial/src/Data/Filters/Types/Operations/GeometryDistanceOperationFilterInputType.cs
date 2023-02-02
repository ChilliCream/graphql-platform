using HotChocolate.Types;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Data.Filters.Spatial;

public class GeometryDistanceOperationFilterInputType
    : ComparableOperationFilterInputType<double>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(SpatialFilterOperations.Geometry).Type<NonNullType<GeometryType>>();
        descriptor.Operation(SpatialFilterOperations.Buffer).Type<FloatType>();
        base.Configure(descriptor);
    }
}
