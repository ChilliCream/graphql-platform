using System;
using System.Reflection;

namespace HotChocolate.Internal
{
    internal static class AttributeExtensions
    {
        public static string GetGraphQLName(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                return member.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }

            if (member is MethodInfo)
            {
                if (member.Name.StartsWith("Get") && member.Name.Length > 3)
                {
                    return NormalizeName(member.Name.Substring(3));
                }
                return NormalizeName(member.Name);
            }

            if (member is PropertyInfo)
            {
                return NormalizeName(member.Name);
            }

            return member.Name;
        }

        private static string NormalizeName(string name)
        {
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToLowerInvariant() +
                    name.Substring(1);
            }
            return name.ToLowerInvariant();
        }
    }
}
