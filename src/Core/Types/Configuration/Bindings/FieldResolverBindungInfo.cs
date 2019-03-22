using System.Reflection;

namespace HotChocolate.Configuration
{
    internal class FieldResolverBindungInfo
    {
        public NameString FieldName { get; set; }

        public MemberInfo FieldMember { get; set; }

        public MemberInfo ResolverMember { get; set; }

        public bool IsValid()
        {
            if (ResolverMember == null)
            {
                return false;
            }

            if (FieldName.HasValue)
            {
                return true;
            }

            if (FieldMember != null)
            {
                return true;
            }

            return false;
        }
    }
}
