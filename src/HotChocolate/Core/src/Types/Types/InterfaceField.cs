using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InterfaceField
        : OutputFieldBase<InterfaceFieldDefinition>
    {
        public InterfaceField(
            InterfaceFieldDefinition definition,
            FieldCoordinate fieldCoordinate,
            bool sortArgumentsByName = false)
            : base(definition, fieldCoordinate, sortArgumentsByName)
        {
        }
    }
}
