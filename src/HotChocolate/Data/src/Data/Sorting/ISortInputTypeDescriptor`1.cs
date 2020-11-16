using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    /// <summary>
    /// The sort input descriptor allows to configure a <see cref="SortInputType"/>.
    /// </summary>
    public interface ISortInputTypeDescriptor<T>
        : ISortInputTypeDescriptor
    {
        /// <summary>
        /// Defines the name of the <see cref="SortInputType{T}"/>.
        /// </summary>
        /// <param name="value">The sort type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        new ISortInputTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="SortInputType{T}"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The sort type description.</param>
        ///
        new ISortInputTypeDescriptor<T> Description(string? value);

        /// <summary>
        /// <para>Defines the sort binding behavior.</para>
        /// <para>
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </para>
        /// </summary>
        /// <param name="bindingBehavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The sort type descriptor will try to infer sorting
        /// from the specified <typeparamref name="T"/>.
        ///
        /// Explicit:
        /// All sorting fields have to be specified explicitly via one of the `Sort`
        /// methods.
        /// </param>
        ISortInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all sorting fields have to be specified explicitly.
        /// </summary>
        ISortInputTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// The sort type will will add
        /// sorting fields for all compatible fields.
        /// </summary>
        ISortInputTypeDescriptor<T> BindFieldsImplicitly();

        /// <summary>
        /// Defines a <see cref="SortField" /> that binds to the specified property.
        /// </summary>
        /// <param name="property">
        /// The property to which a sort field shall be bound.
        /// </param>
        ISortFieldDescriptor Field<TField>(Expression<Func<T, TField>> property);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        new ISortInputTypeDescriptor<T> Ignore(NameString name);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param  name="property">
        /// The property that shall be ignored.
        /// </param>
        ISortInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> property);

        /// <summary>
        /// Adds a directive to this sort input type.
        /// </summary>
        /// <param name="directive">
        /// The directive.
        /// </param>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        new ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        /// <summary>
        /// Adds a directive to this sort input type.
        /// </summary>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        new ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        /// <summary>
        /// Adds a directive to this sort input type.
        /// </summary>
        /// <param name="name">
        /// The name of the directive.
        /// </param>
        /// <param name="arguments">
        /// The directive argument values.
        /// </param>
        new ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
