using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public static class OutputFieldDefinitionBaseExtensions
    {
        public static MemberInfo GetMemberInfoIfPresent(this OutputFieldDefinitionBase definition)
        {
            return (definition as IHasMemberInfo)?.Member;
        }
    }
}
