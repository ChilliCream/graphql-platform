using System;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterDescriptor<T>
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IStringFilterFieldsDescriptor Filter(Expression<Func<T, string>> propertyOrMethod);
    }

    public interface IStringFilterFieldsDescriptor
    {
        IStringFilterFieldsDescriptor BindFilters(BindingBehavior bindingBehavior);
        IStringFilterFieldsDescriptor AllowContains();
        IStringFilterFieldsDescriptor AllowEquals();
        IStringFilterFieldsDescriptor AllowIn();
    }
}
