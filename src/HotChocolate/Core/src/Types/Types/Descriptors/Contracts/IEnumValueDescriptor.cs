using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IEnumValueDescriptor
    : IDescriptor<EnumValueDefinition>
    , IFluent
{
    /// <summary>
    /// Defines the name of the <see cref="EnumValue"/>.
    /// The name represents the public visible enum value name.
    /// </summary>
    /// <param name="value">The enum value name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    IEnumValueDescriptor Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="EnumValue"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The enum value description.</param>
    IEnumValueDescriptor Description(string value);

    /// <summary>
    /// Deprecates the enum value.
    /// </summary>
    /// <param name="reason">The reason why this enum value is deprecated.</param>
    IEnumValueDescriptor Deprecated(string reason);

    /// <summary>
    /// Deprecates the enum value.
    /// </summary>
    IEnumValueDescriptor Deprecated();

    /// <summary>
    /// Ignores the given enum value for the schema creation.
    /// This enum will not be included into the GraphQL schema.
    /// </summary>
    /// <param name="ignore">
    /// The value specifying if this enum value shall be ignored by the type initialization.
    /// </param>
    IEnumValueDescriptor Ignore(bool ignore = true);

    IEnumValueDescriptor Directive<T>(
        T directiveInstance)
        where T : class;

    IEnumValueDescriptor Directive<T>()
        where T : class, new();

    IEnumValueDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
