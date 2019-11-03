using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public interface ISortInputTypeDescriptor<T>
        : IDescriptor<SortInputTypeDefinition>
        , IFluent
    {

        /// <summary>
        /// Defines the name of the <see cref="SortInputType{T}"/>.
        /// </summary>
        /// <param name="value">The sort type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        ISortInputTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="SortInputType{T}"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The sort type description.</param>
        /// 
        ISortInputTypeDescriptor<T> Description(string value);

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
        ISortOperationDescriptor Sortable(
            Expression<Func<T, IComparable>> property);



        ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

        /// <summary>
        /// Defines that the selected property is sortable.
        /// </summary>
        /// <param name="property">
        /// The property that is sortable.
        /// </param>
        ISortObjectOperationDescriptor<TObject> SortableObject<TObject>(
            Expression<Func<T, TObject>> property) where TObject : class;
    }
}
