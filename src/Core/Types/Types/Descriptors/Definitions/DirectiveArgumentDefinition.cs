using System.Reflection;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DirectiveArgumentDefinition
        : ArgumentDefinition
    {
        public PropertyInfo Property { get; set; }
    }
}
