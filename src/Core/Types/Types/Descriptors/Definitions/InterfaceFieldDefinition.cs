using System.Reflection;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceFieldDefinition
        : ComplexFieldDefinitionBase
    {
        public MemberInfo Member { get; set; }
    }
}
