using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

/// <summary>
/// Represents a field of an <see cref="InterfaceType"/>.
/// </summary>
/// <param name="definition">
/// The interface field configuration.
/// </param>
/// <param name="index">
/// The index of the field in the declaring type.
/// </param>
public sealed class InterfaceField(InterfaceFieldConfiguration definition, int index)
    : OutputField(definition, index)
{
    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new InterfaceType DeclaringType => (InterfaceType)base.DeclaringType;
}
