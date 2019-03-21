using System.Reflection;

namespace HotChocolate.Configuration
{
    internal class FieldResolverBindungInfo
    {
        public NameString FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public MemberInfo ResolverMember { get; set; }
    }
}
