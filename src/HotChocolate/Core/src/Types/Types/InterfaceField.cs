using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InterfaceField
        : OutputFieldBase<InterfaceFieldDefinition>
    {
        public InterfaceField(InterfaceFieldDefinition definition, bool sortArgumentsByName = false)
            : base(definition, sortArgumentsByName)
        {
        }
    }
}
