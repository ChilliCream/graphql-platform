using HotChocolate.Types;

namespace HotChocolate.Spatial.Types.Filters
{
    public interface IGeometryFilterFieldDescriptor
    {
        IGeometryFilterFieldDescriptor Name(NameString value);

        IGeometryFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior);

        IGeometryFilterFieldDescriptor BindFiltersExplicitly();

        IGeometryFilterFieldDescriptor BindFiltersImplicitly();

        IGeometryFilterOperationDescriptor AllowDistance();

        IGeometryFilterFieldDescriptor Ignore(bool ignore = true);
    }
}
