using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The filter input descriptor allows to configure a <see cref="FilterInputType"/>.
    /// </summary>
    public interface IFilterInputTypeDescriptor<T>
        : IFilterInputTypeDescriptor
    {
        /// <summary>
        /// Defines the name of the <see cref="FilterInputType{T}"/>.
        /// </summary>
        /// <param name="value">The filter type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        new IFilterInputTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="FilterInputType{T}"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The filter type description.</param>
        ///
        new IFilterInputTypeDescriptor<T> Description(string? value);

        /// <summary>
        /// <para>Defines the filter binding behavior.</para>
        /// <para>
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </para>
        /// </summary>
        /// <param name="bindingBehavior">
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
        IFilterInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

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
        /// Defines a <see cref="FilterField" /> that binds to the specified property.
        /// </summary>
        /// <param name="property">
        /// The property to which a filter field shall be bound.
        /// </param>
        IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> property);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation ID.
        /// </param>
        new IFilterInputTypeDescriptor<T> Ignore(int operationId);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        new IFilterInputTypeDescriptor<T> Ignore(NameString name);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param  name="property">
        /// The property that shall be ignored.
        /// </param>
        IFilterInputTypeDescriptor<T> Ignore(Expression<Func<T, object?>> property);

        /// <summary>
        /// Defines if OR-combinators are allowed for this filter.
        /// </summary>
        /// <param name="allow">
        /// Specifies if OR-combinators are allowed or disallowed.
        /// </param>
        new IFilterInputTypeDescriptor<T> AllowOr(bool allow = true);

        /// <summary>
        /// Defines if AND-combinators are allowed for this filter.
        /// </summary>
        /// <param name="allow">
        /// Specifies if AND-combinators are allowed or disallowed.
        /// </param>
        new IFilterInputTypeDescriptor<T> AllowAnd(bool allow = true);

        /// <summary>
        /// Adds a directive to this filter input type.
        /// </summary>
        /// <param name="directive">
        /// The directive.
        /// </param>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        new IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        /// <summary>
        /// Adds a directive to this filter input type.
        /// </summary>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        new IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        /// <summary>
        /// Adds a directive to this filter input type.
        /// </summary>
        /// <param name="name">
        /// The name of the directive.
        /// </param>
        /// <param name="arguments">
        /// The directive argument values.
        /// </param>
        new IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
