using System;
using System.Reflection;

namespace HotChocolate
{
    internal sealed class MemberResolverInfo
    {
        public MemberResolverInfo(MemberInfo member)
            : this(member, null)
        {
        }

        public MemberResolverInfo(MemberInfo member, string alias)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            Member = member;
            Alias = alias ?? member.Name;
        }

        public MemberInfo Member { get; }
        public string Alias { get; }
    }
}