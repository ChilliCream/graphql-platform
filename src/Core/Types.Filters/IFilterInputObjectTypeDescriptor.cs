using System;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputObjectTypeDescriptor<T>
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IFilterInputObjectTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior);

        IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod);

        IComparableFilterFieldDescriptor Filter<TComparable>(
            Expression<Func<T, TComparable>> propertyOrMethod) where TComparable: IComparable;
    }
}
