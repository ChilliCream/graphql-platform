using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

/// <summary>
/// A fluent configuration API for GraphQL enum types.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The runtime type.
/// </typeparam>"
public interface IEnumTypeDescriptor<TRuntimeType>
    : IDescriptor<EnumTypeConfiguration>
    , IFluent
{
    /// <summary>
    /// Defines the name the enum type shall have.
    /// </summary>
    /// <param name="value">
    /// The name value.
    /// </param>
    IEnumTypeDescriptor<TRuntimeType> Name(string value);

    /// <summary>
    /// Defines the description that the enum type shall have.
    /// </summary>
    /// <param name="value">
    /// The description value.
    /// </param>
    IEnumTypeDescriptor<TRuntimeType> Description(string value);

    /// <summary>
    /// Defines a value that should be included on the enum type.
    /// </summary>
    /// <param name="value">
    /// The value to include.
    /// </param>
    IEnumValueDescriptor Value(TRuntimeType value);

    /// <summary>
    /// Specifies if the enum values shall be inferred or explicitly specified.
    /// </summary>
    /// <param name="behavior">
    /// The binding behavior.
    /// </param>
    IEnumTypeDescriptor<TRuntimeType> BindValues(BindingBehavior behavior);

    /// <summary>
    /// Defines that all enum values have to be specified explicitly.
    /// </summary>
    IEnumTypeDescriptor<TRuntimeType> BindValuesExplicitly();

    /// <summary>
    /// Defines that all enum values shall be inferred
    /// from the associated .Net type,
    /// </summary>
    IEnumTypeDescriptor<TRuntimeType> BindValuesImplicitly();

    /// <summary>
    /// Specifies the enum name comparer that will be used to validate
    /// if an enum name represents an enum value of this type.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer for enum names.
    /// </param>
    IEnumTypeDescriptor NameComparer(IEqualityComparer<string> comparer);

    /// <summary>
    /// Specifies the runtime value comparer that will be used to validate
    /// if a runtime value represents a GraphQL enum value of this type.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer for enum names.
    /// </param>
    IEnumTypeDescriptor ValueComparer(IEqualityComparer<object> comparer);

    /// <summary>
    /// Annotates a directive to this type.
    /// </summary>
    /// <param name="directiveInstance">
    /// The directive that shall be annotated to this type.
    /// </param>
    /// <typeparam name="TDirective">
    /// The type of the directive instance.
    /// </typeparam>
    IEnumTypeDescriptor<TRuntimeType> Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class;

    /// <summary>
    /// Annotates a directive to this type.
    /// </summary>
    /// <typeparam name="TDirective">
    /// The type of the directive instance.
    /// </typeparam>
    IEnumTypeDescriptor<TRuntimeType> Directive<TDirective>()
        where TDirective : class, new();

    /// <summary>
    /// Annotates a directive to this type.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The argument values that the directive instance shall have.
    /// </param>
    IEnumTypeDescriptor<TRuntimeType> Directive(
        string name,
        params ArgumentNode[] arguments);
}
