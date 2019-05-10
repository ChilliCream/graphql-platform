using System;
using System.Collections.Generic;
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
            Expression<Func<T, TComparable>> propertyOrMethod) where TComparable : IComparable;


        IObjectFilterFieldDescriptor Filter<TFilter>(
            Expression<Func<T, object>> propertyOrMethod) where TFilter : IFilterInputType;


        IEnumerableFilterFieldDescriptor Filter(
            Expression<Func<T, IEnumerable<string>>> propertyOrMethod, Action<IStringFilterFieldDescriptor> descriptor);

        IEnumerableFilterFieldDescriptor Filter<TComparable>(
            Expression<Func<T, IEnumerable<TComparable>>> propertyOrMethod, Action<IComparableFilterFieldDescriptor> descriptor) where TComparable : IComparable;


        IEnumerableFilterFieldDescriptor Filter<TFilter>(
            Expression<Func<T, IEnumerable<object>>> propertyOrMethod) where TFilter : IFilterInputType;
    }
}
