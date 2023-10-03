using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
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
    IFilterInputTypeDescriptor<T> Name(string value);

    /// <summary>
    /// Adds explanatory text of the <see cref="FilterInputType{T}"/>
    /// that can be accessed via introspection.
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
    /// The filter type will add filters for all compatible fields.
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
    /// Define a object filter for the selected property.
    /// </summary>
    /// <param name="property">
    /// The property for which a filter shall be applied.
    /// </param>
    IObjectFilterFieldDescriptor<TObject> Object<TObject>(
        Expression<Func<T, TObject>> property)
        where TObject : class;

    /// <summary>
    /// Define a object filter for a IEnumerable of type object
    /// </summary>
    /// <param name="property">
    /// The property for which a filter shall be applied.
    /// </param>
    IArrayFilterFieldDescriptor<TObject> List<TObject>(
        Expression<Func<T, IEnumerable<TObject>>> property)
        where TObject : class;

    /// <summary>
    /// Define a object filter for a IEnumerable of type object
    /// </summary>
    /// <param name="property">
    /// The property for which a filter shall be applied.
    /// </param>
    IArrayFilterFieldDescriptor<ISingleFilter<string>> List(
        Expression<Func<T, IEnumerable<string>>> property);

    /// <summary>
    /// Define a object filter for a IEnumerable of type object
    /// </summary>
    /// <param name="property">
    /// The property for which a filter shall be applied.
    /// </param>
    /// <param name="ignore"></param>
    IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
        Expression<Func<T, IEnumerable<TStruct>>> property,
        RequireStruct<TStruct>? ignore = null)
        where TStruct : struct;

    /// <summary>
    /// Define a object filter for a IEnumerable of type object
    /// </summary>
    /// <param name="property">
    /// The property for which a filter shall be applied.
    /// </param>
    /// <param name="ignore"></param>
    IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
        Expression<Func<T, IEnumerable<TStruct?>>> property,
        RequireStruct<TStruct>? ignore = null)
        where TStruct : struct;

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
        string name,
        params ArgumentNode[] arguments);

    public class RequireStruct<TStruct> where TStruct : struct { }
}
