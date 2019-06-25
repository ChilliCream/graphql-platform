using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputTypeDescriptor<T>
        : IDescriptor<FilterInputTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Defines the filter binding behavior.
        ///
        /// The default binding behavior is set to
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

        IFilterInputTypeDescriptor<T> BindExplicitly();

        IFilterInputTypeDescriptor<T> BindImplicitly();

        IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod);

        IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> propertyOrMethod);

        IComparableFilterFieldDescriptor Filter(
            Expression<Func<T, IComparable>> propertyOrMethod);
    }
}
