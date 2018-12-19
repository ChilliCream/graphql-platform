using System.Reflection;

namespace HotChocolate.Types
{
    internal class InputFieldDescription
        : ArgumentDescription
    {
        public PropertyInfo Property { get; set; }
    }
}
