using System;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputObjectTypeDescriptor<T>
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IStringFilterFieldsDescriptor BindFields(
            BindingBehavior bindingBehavior);

        IStringFilterFieldsDescriptor Filter(Expression<Func<T, string>> propertyOrMethod);
    }


}
