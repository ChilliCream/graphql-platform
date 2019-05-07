using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : ITypeInspector
    {
        private readonly TypeInspector _typeInspector =
            new TypeInspector();

        public virtual IEnumerable<MemberInfo> GetMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => !IsIgnored(m)
                        && !m.IsSpecialName
                        && m.DeclaringType != typeof(object)
                        && m.ReturnType != typeof(void)
                        && m.ReturnType != typeof(Task)))
            {
                yield return method;
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !IsIgnored(p)
                    && p.CanRead
                    && p.DeclaringType != typeof(object)))
            {
                yield return property;
            }
        }

        public virtual IEnumerable<Type> GetResolverTypes(Type sourceType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

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
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            Type returnType = GetReturnType(member);

            if (member.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    member.GetCustomAttribute<GraphQLTypeAttribute>();
                returnType = attribute.Type;
            }

            if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                GraphQLNonNullTypeAttribute attribute =
                    member.GetCustomAttribute<GraphQLNonNullTypeAttribute>();

                return new ClrTypeReference(
                    returnType,
                    context,
                    attribute.IsNullable,
                    attribute.IsElementNullable)
                    .Compile();
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

        public virtual IEnumerable<object> GetEnumValues(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }

            if (enumType != typeof(object) && enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<object>();
            }
            return Enumerable.Empty<object>();
        }

        private static bool IsIgnored(MemberInfo member)
        {
            return member.IsDefined(typeof(GraphQLIgnoreAttribute));
        }

        public Type ExtractType(Type type)
        {
            if (_typeInspector.TryCreate(type, out Utilities.TypeInfo typeInfo))
            {
                return typeInfo.ClrType;
            }
            return type;
        }

        public bool IsSchemaType(Type type) =>
            BaseTypes.IsSchemaType(type);
    }
}
