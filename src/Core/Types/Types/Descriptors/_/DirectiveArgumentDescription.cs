using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveArgumentDescription
        : ArgumentDescription
    {
        public PropertyInfo Property { get; set; }
    }
}
