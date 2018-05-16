using System.Reflection;

namespace HotChocolate.Configuration
{
    public class FieldResolverBindungInfo
    {
        public string FieldName { get; set; }
        public MemberInfo FieldMember { get; set; }
        public MemberInfo ResolverMember { get; set; }
    }
}
