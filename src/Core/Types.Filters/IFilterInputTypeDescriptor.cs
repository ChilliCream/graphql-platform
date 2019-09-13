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

        /// <summary>
        /// Defines that all filters have to be specified explicitly.
        /// </summary>
        IFilterInputTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// The filter type will will add
        /// filters for all compatible fields.
        /// </summary>
        IFilterInputTypeDescriptor<T> BindFieldsImplicitly();

        /// <summary>
        /// Define a string filter for the selected property.
        /// </summary>
        /// <param name="property">
        /// The property for which a filter shall be applied.
        /// </param>
        IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> property);

        /// <summary>
        /// Define a boolean filter for the selected property.
        /// </summary>
        /// <param name="property">
        /// The property for which a filter shall be applied.
        /// </param>
        IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> property);

        /// <summary>
        /// Define a comparable filter for the selected property.
        /// </summary>
        /// <param name="property">
        /// The property for which a filter shall be applied.
        /// </param>
        IComparableFilterFieldDescriptor Filter(
            Expression<Func<T, IComparable>> property);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that hall be ignored.</param>
        IFilterInputTypeDescriptor<T> Ignore(
            Expression<Func<T, object>> property);
    }
}
