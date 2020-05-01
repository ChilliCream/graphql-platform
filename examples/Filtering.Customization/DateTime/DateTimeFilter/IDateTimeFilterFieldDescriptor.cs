
using System;
using System.Reflection;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;

namespace Filtering.Customization
{
    public interface IDateTimeFilterFieldDescriptor
    {

        IDateTimeFilterFieldDescriptor Name(NameString value);

        IDateTimeFilterFieldDescriptor BindFilters(BindingBehavior bindingBehavior);

        IDateTimeFilterFieldDescriptor BindFiltersExplicitly();

        IDateTimeFilterFieldDescriptor BindFiltersImplicitly();

        IDateTimeFilterOperationDescriptor AllowFrom();

        IDateTimeFilterOperationDescriptor AllowTo();

        IDateTimeFilterFieldDescriptor Ignore(bool ignore = true);
    }
}