using System.Reflection;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InterfaceFieldDefinition
        : OutputFieldDefinitionBase
    {
        public MemberInfo Member { get; set; }
    }
}
