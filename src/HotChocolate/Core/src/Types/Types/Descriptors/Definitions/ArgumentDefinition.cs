using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class ArgumentDefinition
        : FieldDefinitionBase<InputValueDefinitionNode>
    {
        public IValueNode? DefaultValue { get; set; }

        public object? NativeDefaultValue { get; set; }

        public ParameterInfo? Parameter { get; set; }
    }
}
