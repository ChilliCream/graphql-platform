using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public sealed class InterfaceField(InterfaceFieldConfiguration definition, int index)
    : OutputFieldBase(definition, index)
{
    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new InterfaceType DeclaringType => (InterfaceType)base.DeclaringType;
}
