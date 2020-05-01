using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputTypeDescriptor<T>
        : IDescriptor<FilterInputTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Defines the name of the <see cref="FilterInputType{T}"/>.
        /// </summary>
        /// <param name="value">The filter type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IFilterInputTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="FilterInputType{T}"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The filter type description.</param>
        ///
        IFilterInputTypeDescriptor<T> Description(string value);

        /// <summary>
        /// Defines the filter binding behavior.
        ///
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
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

        TDesc Filter<TDesc>(
            PropertyInfo property,
            Func<IDescriptorContext, TDesc> factory)
            where TDesc : FilterFieldDescriptorBase;

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that hall be ignored.</param>
        IFilterInputTypeDescriptor<T> Ignore(
            Expression<Func<T, object>> property);

        IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

    }
}
