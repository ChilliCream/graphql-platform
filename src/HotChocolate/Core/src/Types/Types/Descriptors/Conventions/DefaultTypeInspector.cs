using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using CompDefaultValueAttribute = System.ComponentModel.DefaultValueAttribute;
using TypeInfo = HotChocolate.Internal.TypeInfo;

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
        private const string _clone = "<Clone>$";

        private readonly TypeCache _typeCache = new TypeCache();

        private readonly Dictionary<MemberInfo, ExtendedMethodInfo> _methods =
            new Dictionary<MemberInfo, ExtendedMethodInfo>();

        private readonly Dictionary<Type, bool> _records =
            new Dictionary<Type, bool>();

        public DefaultTypeInspector(bool ignoreRequiredAttribute = false)
        {
            IgnoreRequiredAttribute = ignoreRequiredAttribute;
        }

        /// <summary>
        /// Infer type to be non-null if <see cref="RequiredAttribute"/> is found.
        /// </summary>
        public bool IgnoreRequiredAttribute { get; protected set; }

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
        public virtual ExtendedTypeReference GetReturnTypeRef(
            MemberInfo member,
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool ignoreAttributes = false)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            return TypeReference.Create(GetReturnType(member), context, scope);
        }

        /// <inheritdoc />
        public virtual IExtendedType GetReturnType(
            MemberInfo member,
            bool ignoreAttributes = false)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            IExtendedType returnType = ExtendedType.FromMember(member, _typeCache);

            return ignoreAttributes ? returnType : ApplyTypeAttributes(returnType, member);
        }

        /// <inheritdoc />
        public ExtendedTypeReference GetArgumentTypeRef(
            ParameterInfo parameter,
            string? scope = null,
            bool ignoreAttributes = false)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return TypeReference.Create(
                GetArgumentType(
                    parameter,
                    ignoreAttributes),
                TypeContext.Input,
                scope);
        }

        /// <inheritdoc />
        public IExtendedType GetArgumentType(
            ParameterInfo parameter,
            bool ignoreAttributes = false)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            IExtendedType argumentType = GetArgumentTypeInternal(parameter);
            return ignoreAttributes ? argumentType : ApplyTypeAttributes(argumentType, parameter);
        }

        private IExtendedType GetArgumentTypeInternal(ParameterInfo parameter)
        {
            MethodInfo method = (MethodInfo)parameter.Member;

            if (!_methods.TryGetValue(method, out ExtendedMethodInfo? info))
            {
                info = ExtendedType.FromMethod(method, _typeCache);
                _methods[method] = info;
            }

            return info.ParameterTypes[parameter];
        }

        /// <inheritdoc />
        public ExtendedTypeReference GetTypeRef(
            Type type,
            TypeContext context = TypeContext.None,
            string? scope = null) =>
            TypeReference.Create(GetType(type), context, scope);

        /// <inheritdoc />
        public IExtendedType GetType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ExtendedType.FromType(type, _typeCache);
        }

        /// <inheritdoc />
        public IExtendedType GetType(Type type, params bool?[] nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nullable is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ExtendedType extendedType = ExtendedType.FromType(type, _typeCache);

            return nullable is { Length: > 0 }
                ? ExtendedType.Tools.ChangeNullability(extendedType, nullable, _typeCache)
                : extendedType;
        }

        /// <inheritdoc />
        public IExtendedType GetType(Type type, ReadOnlySpan<bool?> nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ExtendedType extendedType = ExtendedType.FromType(type, _typeCache);

            return nullable is { Length: > 0 }
                ? ExtendedType.Tools.ChangeNullability(extendedType, nullable, _typeCache)
                : extendedType;
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
                return Enum.GetValues(enumType).Cast<object>()!;
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

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NativeType<>))
            {
                return type;
            }

            return ExtendedType.Tools.GetNamedType(type) ?? type;
        }

        /// <inheritdoc />
        public bool IsSchemaType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ExtendedType.Tools.IsSchemaType(type);
        }

        /// <inheritdoc />
        public void ApplyAttributes(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider attributeProvider)
        {
            foreach (var attribute in
                GetCustomAttributes<DescriptorAttribute>(attributeProvider, true))
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
                defaultValue = parameter.DefaultValue;
                return true;
            }

            defaultValue = null;
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryGetDefaultValue(PropertyInfo property, out object? defaultValue)
        {
            if (TryGetAttribute(property, out CompDefaultValueAttribute? attribute))
            {
                defaultValue = attribute.Value;
                return true;
            }

            if (TryGetDefaultValueFromConstructor(property, out defaultValue))
            {
                return true;
            }

            defaultValue = null;
            return false;
        }

        /// <inheritdoc />
        public IExtendedType ChangeNullability(IExtendedType type, params bool?[] nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nullable is null)
            {
                throw new ArgumentNullException(nameof(nullable));
            }

            if (nullable.Length > 32)
            {
                throw new ArgumentException(
                    "Types with more than 32 components are not supported.");
            }

            if (nullable.Length == 0)
            {
                return type;
            }

            return ExtendedType.Tools.ChangeNullability(type, nullable, _typeCache);
        }

        private IExtendedType ChangeNullabilityInternal(IExtendedType type, params bool[] nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nullable is null)
            {
                throw new ArgumentNullException(nameof(nullable));
            }

            if (nullable.Length > 32)
            {
                throw new ArgumentException(
                    "Types with more than 32 components are not supported.");
            }

            if (nullable.Length == 0)
            {
                return type;
            }

            Span<bool?> n = stackalloc bool?[nullable.Length];

            for (var i = 0; i < n.Length; i++)
            {
                n[i] = nullable[i];
            }

            return ExtendedType.Tools.ChangeNullability(type, n, _typeCache);
        }

        /// <inheritdoc />
        public IExtendedType ChangeNullability(IExtendedType type, ReadOnlySpan<bool?> nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (nullable.Length > 32)
            {
                throw new ArgumentException(
                    "Types with more than 32 components are not supported.");
            }

            if (nullable.Length == 0)
            {
                return type;
            }

            return ExtendedType.Tools.ChangeNullability(type, nullable, _typeCache);
        }

        /// <inheritdoc />
        public bool?[] CollectNullability(IExtendedType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ExtendedType.Tools.CollectNullability(type);
        }

        /// <inheritdoc />
        public bool CollectNullability(IExtendedType type, Span<bool?> buffer, out int written)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ExtendedType.Tools.CollectNullability(type, buffer, out written);
        }

        /// <inheritdoc />
        public ITypeInfo CreateTypeInfo(Type type) =>
            TypeInfo.Create(GetType(type), _typeCache);

        /// <inheritdoc />
        public ITypeInfo CreateTypeInfo(IExtendedType type) =>
            TypeInfo.Create(type, _typeCache);

        /// <inheritdoc />
        public ITypeFactory CreateTypeFactory(IExtendedType type) =>
            TypeInfo.Create(type, _typeCache);

        /// <inheritdoc />
        public bool TryCreateTypeInfo(
            Type type,
            [NotNullWhen(true)] out ITypeInfo? typeInfo) =>
            TryCreateTypeInfo(GetType(type), out typeInfo);

        /// <inheritdoc />
        public bool TryCreateTypeInfo(
            IExtendedType type,
            [NotNullWhen(true)] out ITypeInfo? typeInfo)
        {
            if (TypeInfo.TryCreate(type, _typeCache, out TypeInfo? t))
            {
                typeInfo = t;
                return true;
            }

            typeInfo = null;
            return false;
        }

        private IExtendedType ApplyTypeAttributes(
            IExtendedType type,
            ICustomAttributeProvider attributeProvider)
        {
            if (TryGetAttribute(attributeProvider, out GraphQLTypeAttribute? typeAttribute))
            {
                return GetType(typeAttribute.Type);
            }

            if (TryGetAttribute(attributeProvider, out GraphQLNonNullTypeAttribute? nullAttribute))
            {
                return ChangeNullabilityInternal(
                    type,
                    nullAttribute.Nullable);
            }

            if (!IgnoreRequiredAttribute &&
                TryGetAttribute(attributeProvider, out RequiredAttribute? _))
            {
                return ChangeNullability(type, false);
            }

            return type;
        }

        private bool TryGetAttribute<T>(
            ICustomAttributeProvider attributeProvider,
            [NotNullWhen(true)] out T? attribute)
            where T : Attribute
        {
            if (attributeProvider is PropertyInfo p &&
                p.DeclaringType is not null &&
                IsRecord(p.DeclaringType))
            {
                if (IsDefinedOnRecord<T>(p, true))
                {
                    attribute = GetCustomAttributeFromRecord<T>(p, true)!;
                    return true;
                }
            }
            else if (attributeProvider.IsDefined(typeof(T), true))
            {
                attribute = attributeProvider
                    .GetCustomAttributes(typeof(T), true)
                    .OfType<T>()
                    .First();
                return true;
            }

            attribute = null;
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
            if (IsCloneMember(member) ||
                IsToString(member) ||
                IsGetHashCode(member) ||
                IsEquals(member))
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

        private bool IsRecord(Type type)
        {
            if (!_records.TryGetValue(type, out bool isRecord))
            {
                isRecord = IsRecord(type.GetMembers());
                _records[type] = isRecord;
            }

            return isRecord;
        }

        private static bool IsRecord(IReadOnlyList<MemberInfo> members)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (IsCloneMember(members[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCloneMember(MemberInfo member) =>
            member.Name.EqualsOrdinal(_clone);

        private IEnumerable<T> GetCustomAttributes<T>(
            ICustomAttributeProvider attributeProvider,
            bool inherit)
            where T : Attribute
        {
            if (attributeProvider is PropertyInfo p &&
                p.DeclaringType is not null &&
                IsRecord(p.DeclaringType))
            {
                return GetCustomAttributesFromRecord<T>(p, inherit);
            }
            else
            {
                return attributeProvider.GetCustomAttributes(true).OfType<T>();
            }
        }

        private IEnumerable<T> GetCustomAttributesFromRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            IEnumerable<T> attributes = Enumerable.Empty<T>();

            if (property.IsDefined(typeof(T)))
            {
                attributes = attributes.Concat(property.GetCustomAttributes<T>(inherit));
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (parameter.Name.EqualsOrdinal(property.Name))
                    {
                        attributes = attributes.Concat(parameter.GetCustomAttributes<T>(inherit));
                    }
                }
            }

            return attributes;
        }

        private T? GetCustomAttributeFromRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            if (property.IsDefined(typeof(T)))
            {
                return property.GetCustomAttribute<T>(inherit);
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (parameter.Name.EqualsOrdinal(property.Name))
                    {
                        return parameter.GetCustomAttribute<T>(inherit);
                    }
                }
            }

            return null;
        }

        private static bool IsDefinedOnRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            if (property.IsDefined(typeof(T), inherit))
            {
                return true;
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (parameter.Name.EqualsOrdinal(property.Name))
                    {
                        return parameter.IsDefined(typeof(T));
                    }
                }
            }

            return false;
        }

        private bool TryGetDefaultValueFromConstructor(
            PropertyInfo property,
            out object? defaultValue)
        {
            defaultValue = null;
            if (IsRecord(property.DeclaringType!))
            {
                ConstructorInfo[] constructors = recordType.GetConstructors();

                if (constructors.Length == 1)
                {
                    foreach (ParameterInfo parameter in constructors[0].GetParameters())
                    {
                        if (parameter.Name.EqualsOrdinal(property.Name))
                        {
                            return TryGetDefaultValue(parameter, out defaultValue);
                        }
                    }
                }
            }

            return false;
        }
    }
}
