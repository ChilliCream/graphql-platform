#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using CompDefaultValueAttribute = System.ComponentModel.DefaultValueAttribute;
using TypeInfo = HotChocolate.Internal.TypeInfo;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The default type inspector implementation that provides helpers to inspect .NET types and
/// infer GraphQL type structures.
/// </summary>
public class DefaultTypeInspector : Convention, ITypeInspector
{
    private const string _toString = "ToString";
    private const string _getHashCode = "GetHashCode";
    private const string _equals = "Equals";
    private const string _clone = "<Clone>$";

    private readonly TypeCache _typeCache = new();
    private readonly Dictionary<MemberInfo, ExtendedMethodInfo> _methods = new();

    public DefaultTypeInspector(bool ignoreRequiredAttribute = false)
    {
        IgnoreRequiredAttribute = ignoreRequiredAttribute;
    }

    /// <summary>
    /// Infer type to be non-null if <see cref="RequiredAttribute"/> is found.
    /// </summary>
    public bool IgnoreRequiredAttribute { get; protected set; }

    /// <inheritdoc />
    public virtual IEnumerable<MemberInfo> GetMembers(Type type)
        => GetMembers(type, false);

    /// <inheritdoc />
    public virtual IEnumerable<MemberInfo> GetMembers(Type type, bool includeIgnored)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return GetMembersInternal(type, includeIgnored);
    }

    /// <inheritdoc />
    public virtual bool IsMemberIgnored(MemberInfo member)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return member.IsDefined(typeof(GraphQLIgnoreAttribute));
    }

    private IEnumerable<MemberInfo> GetMembersInternal(Type type, bool includeIgnored)
    {
        foreach (MemberInfo member in type.GetMembers(Instance | Public))
        {
            if (CanBeHandled(member, includeIgnored))
            {
                yield return member;
            }
        }
    }

    /// <inheritdoc />
    public virtual ITypeReference GetReturnTypeRef(
        MemberInfo member,
        TypeContext context = TypeContext.None,
        string? scope = null,
        bool ignoreAttributes = false)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        ITypeReference typeRef = TypeReference.Create(GetReturnType(member), context, scope);

        if (!ignoreAttributes &&
            TryGetAttribute(member, out GraphQLTypeAttribute? attribute) &&
            attribute.TypeSyntax is not null)
        {
            return TypeReference.Create(attribute.TypeSyntax, context, scope);
        }

        return typeRef;
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

        return ignoreAttributes
            ? returnType
            : ApplyTypeAttributes(returnType, member);
    }

    /// <inheritdoc />
    public ITypeReference GetArgumentTypeRef(
        ParameterInfo parameter,
        string? scope = null,
        bool ignoreAttributes = false)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        ITypeReference typeRef = TypeReference.Create(
            GetArgumentType(
                parameter,
                ignoreAttributes),
            TypeContext.Input,
            scope);

        if (!ignoreAttributes &&
            TryGetAttribute(parameter, out GraphQLTypeAttribute? attribute) &&
            attribute.TypeSyntax is not null)
        {
            return TypeReference.Create(attribute.TypeSyntax, TypeContext.Input, scope);
        }

        return typeRef;
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
        return ignoreAttributes
            ? argumentType
            : ApplyTypeAttributes(argumentType, parameter);
    }

    private IExtendedType GetArgumentTypeInternal(ParameterInfo parameter)
    {
        var method = (MethodInfo)parameter.Member;

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
            throw new ArgumentNullException(nameof(nullable));
        }

        var extendedType = ExtendedType.FromType(type, _typeCache);

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

        var extendedType = ExtendedType.FromType(type, _typeCache);

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

    public virtual MemberInfo? GetNodeIdMember(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return GetMembers(type)
            .FirstOrDefault(
                member =>
                    member.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    member.Name.Equals("GetId", StringComparison.OrdinalIgnoreCase) ||
                    member.Name.Equals("GetIdAsync", StringComparison.OrdinalIgnoreCase));
    }

    public virtual MethodInfo? GetNodeResolverMethod(Type nodeType, Type? resolverType = null)
    {
        if (nodeType == null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        // if we are inspecting the node type itself the method mus be static and does
        // not need to include the node name.
        if (resolverType is null)
        {
            return nodeType
                .GetMembers(Static | Public | FlattenHierarchy)
                .OfType<MethodInfo>()
                .FirstOrDefault(m => IsPossibleNodeResolver(m, nodeType));
        }

        // if we have a resolver type on the other hand the load method must
        // include the type name and can be an instance method.
        // first we will check for static load methods.
        MethodInfo? method = resolverType
            .GetMembers(Static | Public | FlattenHierarchy)
            .OfType<MethodInfo>()
            .FirstOrDefault(m => IsPossibleExternalNodeResolver(m, nodeType));

        if (method is not null)
        {
            return method;
        }

        // if there is no static load method we will move on the check
        // for instance load methods.
        return GetMembers(resolverType)
            .OfType<MethodInfo>()
            .FirstOrDefault(m => IsPossibleExternalNodeResolver(m, nodeType));
    }

    private static bool IsPossibleNodeResolver(
        MemberInfo member,
        Type nodeType) =>
        member.IsDefined(typeof(NodeResolverAttribute)) ||
        member.Name.Equals(
            "Get",
            StringComparison.OrdinalIgnoreCase) ||
        member.Name.Equals(
            "GetAsync",
            StringComparison.OrdinalIgnoreCase) ||
        IsPossibleExternalNodeResolver(member, nodeType);

    private static bool IsPossibleExternalNodeResolver(
        MemberInfo member,
        Type nodeType) =>
        member.IsDefined(typeof(NodeResolverAttribute)) ||
        member.Name.Equals(
            $"Get{nodeType.Name}",
            StringComparison.OrdinalIgnoreCase) ||
        member.Name.Equals(
            $"Get{nodeType.Name}Async",
            StringComparison.OrdinalIgnoreCase);

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
        foreach (DescriptorAttribute attr in
            GetCustomAttributes<DescriptorAttribute>(attributeProvider, true)
                .OrderBy(t => t.Order))
        {
            attr.TryConfigure(context, descriptor, attributeProvider);
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
        IExtendedType resultType = type;

        var hasGraphQLTypeAttribute = false;

        if (TryGetAttribute(attributeProvider, out GraphQLTypeAttribute? typeAttribute) &&
            typeAttribute.Type is { } attributeType)
        {
            hasGraphQLTypeAttribute = true;
            resultType = GetType(attributeType);
        }

        if (TryGetAttribute(attributeProvider, out GraphQLNonNullTypeAttribute? nullAttribute))
        {
            resultType = ChangeNullabilityInternal(resultType, nullAttribute.Nullable);
        }

        if (!IgnoreRequiredAttribute &&
            !hasGraphQLTypeAttribute &&
            TryGetAttribute(attributeProvider, out RequiredAttribute? _))
        {
            resultType = ChangeNullability(resultType, false);
        }

        return resultType;
    }

    private bool TryGetAttribute<T>(
        ICustomAttributeProvider attributeProvider,
        [NotNullWhen(true)] out T? attribute)
        where T : Attribute
    {
        if (attributeProvider.IsDefined(typeof(T), true))
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

    private bool CanBeHandled(MemberInfo member, bool includeIgnored)
    {
        if (IsSystemMember(member))
        {
            return false;
        }

        if (!includeIgnored && IsMemberIgnored(member))
        {
            return false;
        }

        if (member.DeclaringType == typeof(object))
        {
            return false;
        }

        if (member is PropertyInfo { CanRead: false } ||
            member is PropertyInfo { IsSpecialName: true } ||
            member is MethodInfo { IsSpecialName: true })
        {
            return false;
        }

        if (member is PropertyInfo property)
        {
            return CanHandleReturnType(member, property.PropertyType) &&
                property.GetIndexParameters().Length == 0;
        }

        if (member is MethodInfo method &&
            CanHandleReturnType(member, method.ReturnType) &&
            method.GetParameters().All(CanHandleParameter))
        {
            return true;
        }

        return false;
    }

    private static bool CanHandleReturnType(MemberInfo member, Type returnType)
    {
        if (returnType == typeof(void) ||
            returnType == typeof(Task) ||
            returnType == typeof(ValueTask))
        {
            return false;
        }

        if (returnType == typeof(object) ||
            returnType == typeof(Task<object>) ||
            returnType == typeof(ValueTask<object>))
        {
            return HasConfiguration(member);
        }

        if (typeof(IAsyncResult).IsAssignableFrom(returnType))
        {
            if (returnType.IsGenericType)
            {
                Type returnTypeDefinition = returnType.GetGenericTypeDefinition();

                if (returnTypeDefinition == typeof(ValueTask<>) ||
                    returnTypeDefinition == typeof(Task<>))
                {
                    return true;
                }
            }

            return false;
        }

        // All other types may cause errors and need to have an explicit configuration.
        if (typeof(ITypeSystemMember).IsAssignableFrom(returnType))
        {
            return HasConfiguration(member);
        }

        // reflection types should also be excluded by default.
        if (typeof(ICustomAttributeProvider).IsAssignableFrom(returnType))
        {
            return HasConfiguration(member);
        }

#if NETSTANDARD2_0
        if (returnType.IsByRef)
#else
        if (returnType.IsByRefLike ||
            returnType.IsByRef)
#endif
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(returnType))
        {
            return HasConfiguration(member);
        }

        return true;
    }

    private static bool CanHandleParameter(ParameterInfo parameter)
    {
        // schema, object type and object field can be injected into a resolver, so
        // we allow these as parameter type.
        if (typeof(ISchema).IsAssignableFrom(parameter.ParameterType) ||
            typeof(IObjectType).IsAssignableFrom(parameter.ParameterType) ||
            typeof(IOutputField).IsAssignableFrom(parameter.ParameterType))
        {
            return true;
        }

        // All other types may cause errors and need to have an explicit configuration.
        if (typeof(ITypeSystemMember).IsAssignableFrom(parameter.ParameterType) ||
            parameter.ParameterType == typeof(object))
        {
            return HasConfiguration(parameter);
        }

        // Async results are not allowed.
        if (parameter.ParameterType == typeof(ValueTask) ||
            parameter.ParameterType == typeof(Task) ||
            typeof(IAsyncResult).IsAssignableFrom(parameter.ParameterType))
        {
            return false;
        }

        if (parameter.ParameterType.IsGenericType)
        {
            Type parameterTypeDefinition = parameter.ParameterType.GetGenericTypeDefinition();

            if (parameterTypeDefinition == typeof(ValueTask<>) ||
                parameterTypeDefinition == typeof(Task<>))
            {
                return false;
            }
        }

        // reflection types should also be excluded by default.
        if (typeof(ICustomAttributeProvider).IsAssignableFrom(parameter.ParameterType))
        {
            return HasConfiguration(parameter);
        }

        // by ref and out will never be allowed
        if (parameter.ParameterType.IsByRef ||
#if !NETSTANDARD2_0
            parameter.ParameterType.IsByRefLike ||
#endif
            parameter.IsOut)
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(parameter.ParameterType))
        {
            return HasConfiguration(parameter);
        }

        return true;
    }

    private static bool HasConfiguration(ICustomAttributeProvider element)
        => element.IsDefined(typeof(GraphQLTypeAttribute), true) ||
            element.IsDefined(typeof(ParentAttribute), true) ||
            element.IsDefined(typeof(ServiceAttribute), true) ||
            element.IsDefined(typeof(GlobalStateAttribute), true) ||
            element.IsDefined(typeof(ScopedServiceAttribute), true) ||
            element.IsDefined(typeof(ScopedStateAttribute), true) ||
            element.IsDefined(typeof(LocalStateAttribute), true) ||
            element.IsDefined(typeof(DescriptorAttribute), true);

    private static bool IsSystemMember(MemberInfo member)
    {
        if (member is MethodInfo m &&
            (m.Name.EqualsOrdinal(_toString) ||
                m.Name.EqualsOrdinal(_getHashCode) ||
                m.Name.EqualsOrdinal(_equals) ||
                m.Name.EqualsOrdinal(_clone)))
        {
            return true;
        }

        return false;
    }

    private IEnumerable<T> GetCustomAttributes<T>(
        ICustomAttributeProvider attributeProvider,
        bool inherit)
        where T : Attribute
        => attributeProvider.GetCustomAttributes(inherit).OfType<T>();

    private bool TryGetDefaultValueFromConstructor(
        PropertyInfo property,
        out object? defaultValue)
    {
        defaultValue = null;
        ConstructorInfo[] constructors = property.DeclaringType!.GetConstructors();

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

        return false;
    }
}
