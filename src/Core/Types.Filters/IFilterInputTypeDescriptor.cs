using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputTypeDescriptor<T>
        : IDescriptor<FilterInputTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Defines the filter binding behavior.
        ///
        /// The default binding behaviour is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The filter type descriptor will try to infer the filters
        /// from the specified <typeparamref name="T"/>.
        ///
        /// Explicit:
        /// All filters have to be specified explicitly via one of the `Filter`
        /// methods.
        /// </param>
        IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior);

        IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod);

        IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> propertyOrMethod);

        IComparableFilterFieldDescriptor Filter(
            Expression<Func<T, IComparable>> propertyOrMethod);

        /*

        IObjectFilterFieldDescriptor Filter<TFilter>(
            Expression<Func<T, object>> propertyOrMethod)
            where TFilter : IFilterInputType;

        IEnumerableFilterFieldDescriptor Filter(
            Expression<Func<T, IEnumerable<string>>> propertyOrMethod,
            Action<IStringFilterFieldDescriptor> descriptor);

        IEnumerableFilterFieldDescriptor Filter<TComparable>(
            Expression<Func<T, IEnumerable<TComparable>>> propertyOrMethod,
            Action<IComparableFilterFieldDescriptor> descriptor)
            where TComparable : IComparable;


        IEnumerableFilterFieldDescriptor Filter<TFilter>(
            Expression<Func<T, IEnumerable<object>>> propertyOrMethod)
            where TFilter : IFilterInputType;
             */
    }
}
