using System.Reflection;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InputFieldDefinition : ArgumentDefinition
    {
        public PropertyInfo Property { get; set; }
    }
}
