
using System;
using System.Reflection;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;

namespace Filtering.Customization
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