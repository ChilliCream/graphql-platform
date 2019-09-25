using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    public interface ISortInputTypeDescriptor<T>
        : IDescriptor<SortInputTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Defines the sort binding behavior.
        ///
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The sort type descriptor will try to infer the sortable fields
        /// from the specified <typeparamref name="T"/>.
        ///
        /// Explicit:
        /// All sortable fields have to be specified explicitly by specifying
        /// which field is sortable.
        /// </param>
        ISortInputTypeDescriptor<T> BindFields(
            BindingBehavior behavior);

        /// <summary>
        /// Defines that all sortable fields have to be specified explicitly by specifying
        /// which field is sortable.
        /// </summary>
        ISortInputTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// Defines that the sort type descriptor will try to infer the sortable fields
        /// from the specified <typeparamref name="T"/>.
        /// </summary>
        ISortInputTypeDescriptor<T> BindFieldsImplicitly();

        /// <summary>
        /// Defines that the selected property is sortable.
        /// </summary>
        /// <param name="property">
        /// The property that is sortable.
        /// </param>
        ISortFieldDescriptor Sortable(
            Expression<Func<T, IComparable>> property);
    }
}
