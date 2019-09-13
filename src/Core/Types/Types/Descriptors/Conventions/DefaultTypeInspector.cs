using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using HotChocolate.Properties;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : ITypeInspector
    {
        private const string _toString = "ToString";
        private const string _getHashCode = "GetHashCode";

        private readonly TypeInspector _typeInspector =
            new TypeInspector();

        public virtual IEnumerable<MemberInfo> GetMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return GetMembersInternal(type);
        }

        private IEnumerable<MemberInfo> GetMembersInternal(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => !IsIgnored(m)
                        && !m.IsSpecialName
                        && m.DeclaringType != typeof(object)
                        && m.ReturnType != typeof(void)
                        && m.ReturnType != typeof(Task)
                        && m.ReturnType != typeof(object)
                        && m.ReturnType != typeof(Task<object>)
                        && m.GetParameters().All(t =>
                            t.ParameterType != typeof(object))))
            {
                yield return method;
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !IsIgnored(p)
                    && p.CanRead
                    && p.DeclaringType != typeof(object)
                    && p.PropertyType != typeof(object)))
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

            return GetResolverTypesInternal(sourceType);
        }

        private IEnumerable<Type> GetResolverTypesInternal(Type sourceType)
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
                throw new ArgumentException(
                    TypeResources.DefaultTypeInspector_MemberInvalid,
                    nameof(member));
            }
        }

        public ITypeReference GetArgumentType(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            Type argumentType = parameter.ParameterType;

            if (parameter.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLTypeAttribute>();
                argumentType = attribute.Type;
            }

            if (parameter.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                GraphQLNonNullTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLNonNullTypeAttribute>();

                return new ClrTypeReference(
                    argumentType,
                    TypeContext.Input,
                    attribute.IsNullable,
                    attribute.IsElementNullable)
                    .Compile();
            }

            return new ClrTypeReference(argumentType, TypeContext.Input);
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
            if(IsToString(member) || IsGetHashCode(member))
            {
                return true;
            }
            return member.IsDefined(typeof(GraphQLIgnoreAttribute));
        }

        private static bool IsToString(MemberInfo member) =>
            member is MethodInfo m
            && m.Name.Equals(_toString);

        private static bool IsGetHashCode(MemberInfo member) =>
            member is MethodInfo m
            && m.Name.Equals(_getHashCode)
            && m.GetParameters().Length == 0;

        public Type ExtractType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NativeType<>))
            {
                return type;
            }

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
