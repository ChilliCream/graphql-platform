using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InterfaceField
        : OutputFieldBase<InterfaceFieldDefinition>
    {
        public InterfaceField(InterfaceFieldDefinition definition, int index)
            : base(definition, index)
        {
        }
    }
}
