using System.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using HotChocolate.Properties;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using CompDefaultValueAttribute = System.ComponentModel.DefaultValueAttribute;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspector
        : Convention
        , ITypeInspector
    {
        private const string _toString = "ToString";
        private const string _getHashCode = "GetHashCode";
        private const string _equals = "Equals";

        private readonly Dictionary<MemberInfo, IExtendedMethodTypeInfo> _methods =
            new Dictionary<MemberInfo, IExtendedMethodTypeInfo>();

        public static DefaultTypeInspector Default { get; } =
            new DefaultTypeInspector();

        /// <summary>
        /// Infer type to be non-null if <see cref="RequiredAttribute"/> is found.
        /// </summary>
        public bool RequiredAsNonNull { get; protected set; } = true;

        /// <inheritdoc />
        public virtual IEnumerable<Type> GetResolverTypes(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return GetResolverTypesInternal(type);
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

        /// <inheritdoc />
        public virtual IEnumerable<MemberInfo> GetMembers(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return GetMembersInternal(type);
        }

        private IEnumerable<MemberInfo> GetMembersInternal(Type type) =>
            type.GetMembers(BindingFlags.Instance | BindingFlags.Public).Where(CanBeHandled);

        /// <inheritdoc />
        public virtual ITypeReference GetReturnTypeRef(MemberInfo member, TypeContext context)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            return TypeReference.Create(
                ExtendedTypeRewriter.Rewrite(
                    GetReturnType(member)),
                context);
        }

        /// <inheritdoc />
        public virtual IExtendedType GetReturnType(MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            IExtendedType returnType;

            switch (member)
            {
                case MethodInfo m:
                    if (!_methods.TryGetValue(member, out IExtendedMethodTypeInfo? methodTypeInfo))
                    {
                        methodTypeInfo = m.GetExtendedMethodTypeInfo();
                        _methods[m] = methodTypeInfo;
                    }
                    returnType = methodTypeInfo.ReturnType;
                    break;

                case PropertyInfo p:
                    returnType =  p.GetExtendedReturnType();
                    break;

                default:
                    throw new ArgumentException(
                        TypeResources.DefaultTypeInspector_MemberInvalid,
                        nameof(member));
            }

            if (member.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    member.GetCustomAttribute<GraphQLTypeAttribute>()!;
                returnType = ExtendedType.FromType(attribute.Type);
            }

            if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                GraphQLNonNullTypeAttribute attribute =
                    member.GetCustomAttribute<GraphQLNonNullTypeAttribute>()!;
                return returnType.RewriteNullability(
                    attribute.Nullable.Select(t => new bool?(t)).ToArray());
            }

            if (RequiredAsNonNull && member.IsDefined(typeof(RequiredAttribute)))
            {
                returnType = returnType.RewriteNullability(false);
            }

            return returnType;
        }

        /// <inheritdoc />
        public ITypeReference GetArgumentTypeRef(ParameterInfo parameter)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return TypeReference.Create(
                ExtendedTypeRewriter.Rewrite(
                    GetArgumentType(parameter)),
                TypeContext.Input);
        }

        /// <inheritdoc />
        public IExtendedType GetArgumentType(ParameterInfo parameter)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            IExtendedType argumentType = GetArgumentTypeInternal(parameter);

            if (parameter.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                GraphQLTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLTypeAttribute>()!;
                return ExtendedType.FromType(attribute.Type);
            }

            if (parameter.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                GraphQLNonNullTypeAttribute attribute =
                    parameter.GetCustomAttribute<GraphQLNonNullTypeAttribute>()!;
                return argumentType.RewriteNullability(
                    attribute.Nullable.Select(t => new bool?(t)).ToArray());
            }

            return argumentType;
        }

        public ClrTypeReference GetTypeRef(
            Type type,
            TypeContext context = TypeContext.None,
            string? scope = null)
        {
            throw new NotImplementedException();
        }

        private IExtendedType GetArgumentTypeInternal(ParameterInfo parameter)
        {
            MethodInfo method = (MethodInfo)parameter.Member;

            if (!_methods.TryGetValue(method, out IExtendedMethodTypeInfo? info))
            {
                info = method.GetExtendedMethodTypeInfo();
            }

            return info.ParameterTypes[parameter];
        }

        /// <inheritdoc />
        public virtual IEnumerable<object> GetEnumValues(Type enumType)
        {
            if (enumType is null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }

            if (enumType != typeof(object) && enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<object>();
            }

            return Enumerable.Empty<object>();
        }

        /// <inheritdoc />
        public MemberInfo? GetEnumValueMember(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type enumType = value.GetType();

            if (enumType.IsEnum)
            {
                return enumType.GetMember(value.ToString()!).FirstOrDefault();
            }

            return null;
        }

        /// <inheritdoc />
        public Type ExtractNamedType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NativeType<>))
            {
                return type;
            }

            if (Internal.TypeInfo.TryCreate(type, out Internal.TypeInfo? typeInfo))
            {
                return typeInfo.NamedType;
            }

            return type;
        }

        /// <inheritdoc />
        public bool IsSchemaType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return BaseTypes.IsSchemaType(type);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
        {
            if (parameter.IsDefined(typeof(CompDefaultValueAttribute)))
            {
                defaultValue = parameter.GetCustomAttribute<CompDefaultValueAttribute>()!.Value;
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

        public virtual bool TryGetDefaultValue(PropertyInfo property, out object? defaultValue)
        {
            if (property.IsDefined(typeof(CompDefaultValueAttribute)))
            {
                defaultValue = property.GetCustomAttribute<CompDefaultValueAttribute>()!.Value;
                return true;
            }

            defaultValue = null;
            return false;
        }

        public IExtendedType RewriteNullability(IExtendedType type, params bool?[] nullable)
        {
            throw new NotImplementedException();
        }

        public IExtendedType GetType(Type type)
        {
            throw new NotImplementedException();
        }

        public ITypeInfo CreateTypeInfo(Type type)
        {
            throw new NotImplementedException();
        }

        public ITypeInfo CreateTypeInfo(IExtendedType type)
        {
            throw new NotImplementedException();
        }

        public bool TryCreateTypeInfo(
            Type type,
            [NotNullWhen(true)] out ITypeInfo? typeInfo)
        {
            if(Internal.TypeInfo.TryCreate(type, out Internal.TypeInfo? t))
            {
                typeInfo = t;
                return true;
            }

            typeInfo = null;
            return false;
        }

        public bool TryCreateTypeInfo(
            IExtendedType type,
            [NotNullWhen(true)]out ITypeInfo? typeInfo)
        {
            if(Internal.TypeInfo.TryCreate(type, out Internal.TypeInfo? t))
            {
                typeInfo = t;
                return true;
            }

            typeInfo = null;
            return false;
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
    }
}
