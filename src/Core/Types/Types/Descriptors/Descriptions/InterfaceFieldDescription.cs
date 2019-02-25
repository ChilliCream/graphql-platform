using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceFieldDescription
        : ComplexFieldDescriptionBase
    {
        public MemberInfo Member { get; set; }
    }
}
