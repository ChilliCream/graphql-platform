using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputTypeDescriptor
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
        IFilterInputTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="FilterInputType{T}"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The filter type description.</param>
        /// 
        IFilterInputTypeDescriptor Description(string value);

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
        IFilterInputTypeDescriptor BindFields(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filters have to be specified explicitly.
        /// </summary>
        IFilterInputTypeDescriptor BindFieldsExplicitly();

        /// <summary>
        /// The filter type will will add
        /// filters for all compatible fields.
        /// </summary>
        IFilterInputTypeDescriptor BindFieldsImplicitly();


        IFilterInputTypeDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IFilterInputTypeDescriptor Directive<TDirective>()
             where TDirective : class, new();

        IFilterInputTypeDescriptor Directive(
             NameString name,
             params ArgumentNode[] arguments);
    }
}
