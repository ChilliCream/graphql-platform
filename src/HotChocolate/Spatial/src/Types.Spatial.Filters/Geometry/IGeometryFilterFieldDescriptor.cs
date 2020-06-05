using HotChocolate.Types;

namespace HotChocolate.Types.Spatial.Filters
{
    public interface IGeometryFilterFieldDescriptor
    {
        IGeometryFilterFieldDescriptor Name(NameString value);

        IGeometryFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior);

        IGeometryFilterFieldDescriptor BindFiltersExplicitly();

        IGeometryFilterFieldDescriptor BindFiltersImplicitly();

        IGeometryFilterOperationDescriptor AllowArea();

        IGeometryFilterOperationDescriptor AllowCrosses();

        IGeometryFilterOperationDescriptor AllowDistance();

        IGeometryFilterOperationDescriptor AllowIntersects();

        IGeometryFilterOperationDescriptor AllowLength();

        IGeometryFilterOperationDescriptor AllowWithin();

        IGeometryFilterFieldDescriptor Ignore(bool ignore = true);
    }
}
