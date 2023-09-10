using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public class InterfaceField : OutputFieldBase<InterfaceFieldDefinition>
{
    public InterfaceField(InterfaceFieldDefinition definition, int index)
        : base(definition, index)
    {
    }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new InterfaceType DeclaringType => (InterfaceType)base.DeclaringType;
}
