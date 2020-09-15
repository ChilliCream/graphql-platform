using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    /// <summary>
    /// The sort input descriptor allows to configure a <see cref="SortInputType"/>.
    /// </summary>
    public interface ISortInputTypeDescriptor
        : IDescriptor<SortInputTypeDefinition>
        , IFluent
        , IHasRuntimeType
    {
        /// <summary>
        /// Defines the name of the <see cref="SortInputType{T}"/>.
        /// </summary>
        /// <param name="value">The sort type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        ISortInputTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="SortInputType{T}"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The sort type description.</param>
        ///
        ISortInputTypeDescriptor Description(string? value);

        /// <summary>
        /// Defines a <see cref="SortField" /> with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        ISortFieldDescriptor Field(NameString name);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="name">
        /// The name of the field.
        /// </param>
        ISortInputTypeDescriptor Ignore(NameString name);

        /// <summary>
        /// Adds a directive to this sort input type.
        /// </summary>
        /// <param name="directive">
        /// The directive.
        /// </param>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        ISortInputTypeDescriptor Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        /// <summary>
        /// Adds a directive to this sort input type.
        /// </summary>
        /// <typeparam name="TDirective">
        /// The type of the directive.
        /// </typeparam>
        ISortInputTypeDescriptor Directive<TDirective>()
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
        ISortInputTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
