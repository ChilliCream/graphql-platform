using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ArgumentDefinition
        : FieldDefinitionBase<InputValueDefinitionNode>
    {
        public IValueNode DefaultValue { get; set; }

        public object NativeDefaultValue { get; set; }
    }
}
