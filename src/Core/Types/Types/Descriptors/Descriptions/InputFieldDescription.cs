using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public class InputFieldDescription
        : ArgumentDescription
    {
        public PropertyInfo Property { get; set; }
    }
}
