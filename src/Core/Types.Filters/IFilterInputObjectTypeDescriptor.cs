using System;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputObjectTypeDescriptor<T>
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IStringFilterFieldDescriptor BindFields(
            BindingBehavior bindingBehavior);

        IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod);
    }
}
