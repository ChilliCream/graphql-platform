using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescription
        : FieldDescriptionBase<InputValueDefinitionNode>
    {
        public IValueNode DefaultValue { get; set; }

        public object NativeDefaultValue { get; set; }
    }
}
