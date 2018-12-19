using System.Reflection;

namespace HotChocolate.Types
{
    internal class DirectiveArgumentDescription
        : ArgumentDescription
    {
        public PropertyInfo Property { get; set; }
    }
}
