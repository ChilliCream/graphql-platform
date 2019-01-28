using System.Reflection;

namespace HotChocolate.Types
{
    internal class InterfaceFieldDescription
        : ObjectFieldDescriptionBase
    {
        public MemberInfo ClrMember { get; set; }
    }
}
