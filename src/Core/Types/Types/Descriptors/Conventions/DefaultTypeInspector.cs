using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : ITypeInspector
    {
        public IEnumerable<MemberInfo> GetMembers(Type type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Type> GetResolverTypes(Type sourceType)
        {
            if (sourceType.IsDefined(typeof(GraphQLResolverAttribute)))
            {
                return sourceType
                    .GetCustomAttributes(typeof(GraphQLResolverAttribute))
                    .OfType<GraphQLResolverAttribute>()
                    .SelectMany(attr => attr.ResolverTypes);
            }
            return Enumerable.Empty<Type>();
        }

        public virtual ITypeReference GetReturnType(
            MemberInfo member,
            TypeContext context)
        {
            Type returnType = GetReturnType(member);

            if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                var attribute =
                    member.GetCustomAttribute<GraphQLNonNullTypeAttribute>();

                return new ClrTypeReference(
                    returnType,
                    context,
                    attribute.IsNullable,
                    attribute.IsElementNullable);
            }

            return new ClrTypeReference(returnType, context);
        }

        protected static Type GetReturnType(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                return m.ReturnType;
            }
            else if (member is PropertyInfo p)
            {
                return p.PropertyType;
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("TODO", nameof(member));
            }
        }

        public IEnumerable<object> GetEnumValues(Type enumType)
        {
            if (enumType != typeof(object) && enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<object>();
            }
            return Enumerable.Empty<object>();
        }
    }
}
