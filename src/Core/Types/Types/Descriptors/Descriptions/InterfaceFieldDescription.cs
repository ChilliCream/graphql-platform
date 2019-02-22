using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public class InterfaceFieldDescription
        : OutputFieldDescriptionBase
    {
        public MemberInfo Member { get; set; }
    }
}
