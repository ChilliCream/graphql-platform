using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : ITypeInspector
    {
        private const string _toString = "ToString";
        private const string _getHashCode = "GetHashCode";
        private const string _equals = "Equals";

        private readonly TypeInspector _typeInspector =
            new TypeInspector();
        private readonly ConcurrentDictionary<MemberInfo, IExtendedMethodTypeInfo> _methods =
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
                    .Where(m => CanBeHandled(m)))
            {
                yield return method;
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(p => CanBeHandled(p)))
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

        public virtual ITypeReference GetReturnType(MemberInfo member, TypeContext context)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            Type returnType = GetReturnType(member);

            if (member.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                var attribute = member.GetCustomAttribute<GraphQLTypeAttribute>();
                returnType = attribute.Type;
            }
            else if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                var attribute = member.GetCustomAttribute<GraphQLNonNullTypeAttribute>();

                return new ClrTypeReference(
                    returnType,
                    context,
                    attribute.IsNullable,
                    attribute.IsElementNullable)
                    .Compile();
            }
            else if (member.IsDefined(typeof(RequiredAttribute)))
            {
                return new ClrTypeReference(
                    returnType,
                    context,
                    false)
                    .Compile();
            }

            return new ClrTypeReference(returnType, context);
        }

        protected Type GetReturnType(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                IExtendedMethodTypeInfo info = m.GetExtendedMethodTypeInfo();
                _methods.TryAdd(m, info);
                return ExtendedTypeRewriter.Rewrite(info.ReturnType);
            }
            else if (member is PropertyInfo p)
            {
                return ExtendedTypeRewriter.Rewrite(p.GetExtendedReturnType());
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

            Type argumentType = GetArgumentTypeInternal(parameter);

            if (parameter.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLTypeAttribute>();
                argumentType = attribute.Type;
            }
            else if (parameter.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
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
            else if (parameter.IsDefined(typeof(RequiredAttribute)))
            {
                return new ClrTypeReference(
                    argumentType,
                    TypeContext.Input,
                    true)
                    .Compile();
            }

            return new ClrTypeReference(argumentType, TypeContext.Input);
        }

        private Type GetArgumentTypeInternal(ParameterInfo parameter)
        {
            MethodInfo method = (MethodInfo)parameter.Member;

            if (!_methods.TryGetValue(method, out IExtendedMethodTypeInfo info))
            {
                info = method.GetExtendedMethodTypeInfo();
            }

            return ExtendedTypeRewriter.Rewrite(info.ParameterTypes[parameter]);
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

        public MemberInfo GetEnumValueMember(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type enumType = value.GetType();
            if (enumType.IsEnum)
            {
                return enumType.GetMember(value.ToString()).FirstOrDefault();
            }
            return null;
        }

        private static bool CanBeHandled(MemberInfo member)
        {
            if (IsIgnored(member))
            {
                return false;
            }

            if (member is PropertyInfo property)
            {
                if (!property.CanRead)
                {
                    return false;
                }

                if (property.DeclaringType == typeof(object)
                    || property.PropertyType == typeof(object))
                {
                    return HasConfiguration(property);
                }

                return true;
            }

            if (member is MethodInfo method)
            {
                if (method.IsSpecialName
                    || method.ReturnType == typeof(void)
                    || method.ReturnType == typeof(Task)
                    || method.ReturnType == typeof(ValueTask)
                    || method.DeclaringType == typeof(object))
                {
                    return false;
                }

                if ((method.ReturnType == typeof(object)
                    || method.ReturnType == typeof(Task<object>)
                    || method.ReturnType == typeof(ValueTask<object>))
                    && !HasConfiguration(method))
                {
                    return false;
                }

                if (method.GetParameters().Any(t => t.ParameterType == typeof(object)))
                {
                    return method.GetParameters()
                        .Where(t => t.ParameterType == typeof(object))
                        .All(t => HasConfiguration(t));
                }

                return true;
            }

            return false;
        }

        private static bool HasConfiguration(ICustomAttributeProvider element)
        {
            return element.IsDefined(typeof(GraphQLTypeAttribute), true)
                || element.GetCustomAttributes(typeof(DescriptorAttribute), true).Length > 0;
        }

        private static bool IsIgnored(MemberInfo member)
        {
            if (IsToString(member) || IsGetHashCode(member) || IsEquals(member))
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

        private static bool IsEquals(MemberInfo member) =>
            member is MethodInfo m
            && m.Name.Equals(_equals);

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

        public bool IsSchemaType(Type type) => BaseTypes.IsSchemaType(type);

        public void ApplyAttributes(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider attributeProvider)
        {
            foreach (var attribute in attributeProvider.GetCustomAttributes(true)
                .OfType<DescriptorAttribute>())
            {
                attribute.TryConfigure(context, descriptor, attributeProvider);
            }
        }

        public virtual bool TryGetDefaultValue(ParameterInfo parameter, out object defaultValue)
        {
            if (parameter.IsDefined(typeof(DefaultValueAttribute)))
            {
                defaultValue = parameter.GetCustomAttribute<DefaultValueAttribute>().Value;
                return true;
            }

            if (parameter.HasDefaultValue)
            {
                defaultValue = parameter.RawDefaultValue;
                return true;
            }

            defaultValue = null;
            return false;
        }

        public virtual bool TryGetDefaultValue(PropertyInfo parameter, out object defaultValue)
        {
            if (parameter.IsDefined(typeof(DefaultValueAttribute)))
            {
                defaultValue = parameter.GetCustomAttribute<DefaultValueAttribute>().Value;
                return true;
            }

            defaultValue = null;
            return false;
        }
    }
}
