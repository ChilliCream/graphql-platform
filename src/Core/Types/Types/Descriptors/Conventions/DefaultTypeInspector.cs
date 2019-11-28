using System.Collections.Concurrent;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : ITypeInspector
    {
        private const string _toString = "ToString";
        private const string _getHashCode = "GetHashCode";

        private readonly TypeInspector _typeInspector =
            new TypeInspector();

        private ConcurrentDictionary<MemberInfo, IExtendedMethodTypeInfo> _methods =
            new ConcurrentDictionary<MemberInfo, IExtendedMethodTypeInfo>();

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

            IExtendedType returnType = GetReturnType(member);

            if (member.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    member.GetCustomAttribute<GraphQLTypeAttribute>();
                returnType = ExtendedType.FromType(attribute.Type);
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

        protected IExtendedType GetReturnType(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                IExtendedMethodTypeInfo info = m.GetExtendeMethodTypeInfo();
                _methods.TryAdd(m, info);
                return info.ReturnType;
            }
            else if (member is PropertyInfo p)
            {
                return p.GetExtendedReturnType();
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

            IExtendedType argumentType = GetArgumentReturnType(parameter);

            if (parameter.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLTypeAttribute>();
                argumentType = ExtendedType.FromType(attribute.Type);
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

        private IExtendedType GetArgumentReturnType(ParameterInfo parameter)
        {
            MethodInfo method = (MethodInfo)parameter.Member;

            if (!_methods.TryGetValue(method, out IExtendedMethodTypeInfo info))
            {
                info = method.GetExtendeMethodTypeInfo();
            }

            return info.ParameterTypes[parameter];
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
            if (IsToString(member) || IsGetHashCode(member))
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

            if (_typeInspector.TryCreate(
                ExtendedType.FromType(type),
                out Utilities.TypeInfo? typeInfo))
            {
                return typeInfo!.Type;
            }

            return type;
        }

        public bool IsSchemaType(Type type) =>
            BaseTypes.IsSchemaType(type);
    }
}
