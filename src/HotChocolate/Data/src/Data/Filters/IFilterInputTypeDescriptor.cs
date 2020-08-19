using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The filter input descriptor allows to configure a <see cref="FilterInputType"/>.
    /// </summary>
    public interface IFilterInputTypeDescriptor
        : IDescriptor<FilterInputTypeDefinition>
        , IFluent
        , IHasRuntimeType
    {
        /// <summary>
        /// Defines the name of the <see cref="FilterInputType{T}"/>.
        /// </summary>
        /// <param name="value">The filter type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IFilterInputTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="FilterInputType{T}"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The filter type description.</param>
        ///
        IFilterInputTypeDescriptor Description(string? value);

        /// <summary>
        /// Defines a <see cref="FilterOperationField" /> field.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation ID.
        /// </param>
        IFilterOperationFieldDescriptor Operation(int operationId);

        /// <summary>
        /// Defines a <see cref="FilterField" /> with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        IFilterFieldDescriptor Field(NameString name);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="operationId">
        /// The internal operation ID.
        /// </param>
        IFilterInputTypeDescriptor Ignore(int operationId);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        IFilterInputTypeDescriptor Ignore(NameString name);

        /// <summary>
        /// Defines if OR-combinators are allowed for this filter.
        /// </summary>
        /// <param name="allow">
        /// Specifies if OR-combinators are allowed or disallowed.
        /// </param>
        IFilterInputTypeDescriptor AllowOr(bool allow = true);

        /// <summary>
        /// Defines if AND-combinators are allowed for this filter.
        /// </summary>
        /// <param name="allow">
        /// Specifies if AND-combinators are allowed or disallowed.
        /// </param>
        IFilterInputTypeDescriptor AllowAnd(bool allow = true);

        /// <summary>
        /// Adds a directive to this filter input type.
        /// </summary>
        /// <param name="directive">
        /// The directive.
        /// </param>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        IFilterInputTypeDescriptor Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        /// <summary>
        /// Adds a directive to this filter input type.
        /// </summary>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        IFilterInputTypeDescriptor Directive<TDirective>()
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
        IFilterInputTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
