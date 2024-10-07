#nullable enable

using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GreenDonut;
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
public class DefaultTypeInspector(bool ignoreRequiredAttribute = false) : Convention, ITypeInspector
{
    private const string _toString = "ToString";
    private const string _getHashCode = "GetHashCode";
    private const string _compareTo = "CompareTo";
    private const string _equals = "Equals";
    private const string _clone = "<Clone>$";

    private readonly TypeCache _typeCache = new();
    private readonly Dictionary<MemberInfo, ExtendedMethodInfo> _methods = new();
    private readonly ConcurrentDictionary<(Type, bool, bool), MemberInfo[]> _memberCache = new();

    /// <summary>
    /// Infer type to be non-null if <see cref="RequiredAttribute"/> is found.
    /// </summary>
    public bool IgnoreRequiredAttribute { get; protected set; } = ignoreRequiredAttribute;

    /// <inheritdoc />
    public ReadOnlySpan<MemberInfo> GetMembers(
        Type type,
        bool includeIgnored = false,
        bool includeStatic = false,
        bool allowObject = false)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var cacheKey = (type, includeIgnored, includeStatic);

        if (_memberCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var members = includeStatic
            ? type.GetMembers(Instance | Static | Public)
            : type.GetMembers(Instance | Public);

        var temp = ArrayPool<MemberInfo>.Shared.Rent(members.Length);
        var next = 0;

        foreach (var member in members)
        {
            if (CanBeHandled(member, includeIgnored, allowObject))
            {
                temp[next++] = member;
            }
        }

        var span = temp.AsSpan().Slice(0, next);
        var selectedMembers = new MemberInfo[next];
        span.CopyTo(selectedMembers);
        span.Clear();
        _memberCache.TryAdd(cacheKey, selectedMembers);

        ArrayPool<MemberInfo>.Shared.Return(temp);
        return selectedMembers;
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

    /// <inheritdoc />
    public virtual TypeReference GetReturnTypeRef(
        MemberInfo member,
        TypeContext context = TypeContext.None,
        string? scope = null,
        bool ignoreAttributes = false)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        TypeReference typeRef = TypeReference.Create(GetReturnType(member), context, scope);

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
    public TypeReference GetArgumentTypeRef(
        ParameterInfo parameter,
        string? scope = null,
        bool ignoreAttributes = false)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        TypeReference typeRef = TypeReference.Create(
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
    public virtual IExtendedType GetArgumentType(
        ParameterInfo parameter,
        bool ignoreAttributes = false)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        var argumentType = GetArgumentTypeInternal(parameter);
        return ignoreAttributes
            ? argumentType
            : ApplyTypeAttributes(argumentType, parameter);
    }

    private IExtendedType GetArgumentTypeInternal(ParameterInfo parameter)
    {
        var method = (MethodInfo)parameter.Member;

        if (!_methods.TryGetValue(method, out var info))
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

        return nullable is { Length: > 0, }
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

        return nullable is { Length: > 0, }
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

        return [];
    }

    /// <inheritdoc />
    public MemberInfo? GetEnumValueMember(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var enumType = value.GetType();

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

        foreach (var member in GetMembers(type))
        {
            if (member.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                member.Name.Equals("GetId", StringComparison.OrdinalIgnoreCase) ||
                member.Name.Equals("GetIdAsync", StringComparison.OrdinalIgnoreCase))
            {
                return member;
            }
        }

        return null;
    }

    public virtual MethodInfo? GetNodeResolverMethod(Type nodeType, Type? resolverType = null)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        // if we are inspecting the node type itself the method mus be static and does
        // not need to include the node name.
        if (resolverType is null)
        {
            foreach (var member in nodeType.GetMembers(Static | Public | FlattenHierarchy))
            {
                if (member is MethodInfo m && IsPossibleNodeResolver(m, nodeType))
                {
                    return m;
                }
            }

            // check interfaces
            var interfaceMembers = nodeType
                .GetInterfaces()
                .SelectMany(i => i.GetMembers(Static | Public | FlattenHierarchy));

            foreach (var member in interfaceMembers)
            {
                if (member is MethodInfo m && IsPossibleNodeResolver(m, nodeType))
                {
                    return m;
                }
            }

            return null;
        }

        // if we have a resolver type on the other hand the load method must
        // include the type name and can be an instance method.
        // first we will check for static load methods.
        MethodInfo? method = null;

        foreach (var member in resolverType.GetMembers(Static | Public | FlattenHierarchy))
        {
            if (member is MethodInfo m && IsPossibleExternalNodeResolver(m, nodeType))
            {
                method = m;
                break;
            }
        }

        if (method is not null)
        {
            return method;
        }

        // if there is no static load method we will move on the check
        // for instance load methods.
        foreach (var member in GetMembers(resolverType))
        {
            if (member is MethodInfo m && IsPossibleExternalNodeResolver(m, nodeType))
            {
                return m;
            }
        }

        return null;
    }

    private static bool IsPossibleNodeResolver(
        MemberInfo member,
        Type nodeType) =>
        member.IsDefined(typeof(NodeResolverAttribute)) ||
        member.Name.Equals("Get", StringComparison.OrdinalIgnoreCase) ||
        member.Name.Equals("GetAsync", StringComparison.OrdinalIgnoreCase) ||
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
        var attributes = attributeProvider.GetCustomAttributes(true);
        var temp = ArrayPool<DescriptorAttribute>.Shared.Rent(attributes.Length);
        var i = 0;

        foreach (var attribute in attributes)
        {
            if (attribute is DescriptorAttribute casted)
            {
                temp[i++] = casted;
            }
        }

        Array.Sort(temp, 0, i, DescriptorAttributeComparer.Default);

        var span = temp.AsSpan().Slice(0, i);

        foreach (var attribute in span)
        {
            attribute.TryConfigure(context, descriptor, attributeProvider);
        }

        span.Clear();
        ArrayPool<DescriptorAttribute>.Shared.Return(temp);
    }

    private sealed class DescriptorAttributeComparer : IComparer
    {
        public static DescriptorAttributeComparer Default { get; } = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (y is not DescriptorAttribute attr2)
            {
                return 1;
            }

            if (x is not DescriptorAttribute attr1)
            {
                return -1;
            }

            return attr1.Order.CompareTo(attr2.Order);
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
        if (TypeInfo.TryCreate(type, _typeCache, out var t))
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
        var resultType = type;

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

    private static bool TryGetAttribute<T>(
        ICustomAttributeProvider attributeProvider,
        [NotNullWhen(true)] out T? attribute)
        where T : Attribute
    {
        if (attributeProvider.IsDefined(typeof(T), true))
        {
            foreach (var item in attributeProvider.GetCustomAttributes(typeof(T), true))
            {
                if (item is T casted)
                {
                    attribute = casted;
                    return true;
                }
            }
        }

        attribute = null;
        return false;
    }

    private bool CanBeHandled(
        MemberInfo member,
        bool includeIgnored,
        bool allowObjectType)
    {
        if (IsSystemMember(member))
        {
            return false;
        }

        if (member.IsDefined(typeof(DataLoaderAttribute)) ||
            member.IsDefined(typeof(QueryAttribute)) ||
            member.IsDefined(typeof(MutationAttribute)) ||
            member.IsDefined(typeof(SubscriptionAttribute)))
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

        if (member is PropertyInfo { CanRead: false, } ||
            member is PropertyInfo { IsSpecialName: true, } ||
            member is MethodInfo { IsSpecialName: true, })
        {
            return false;
        }

        if (member is PropertyInfo property)
        {
            return CanHandleReturnType(member, property.PropertyType, allowObjectType) &&
                property.GetIndexParameters().Length == 0;
        }

        if (member is MethodInfo { IsGenericMethodDefinition: false, } method &&
            CanHandleReturnType(member, method.ReturnType, allowObjectType))
        {
            foreach (var parameter in method.GetParameters())
            {
                if (!CanHandleParameter(parameter, allowObjectType))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    private static bool CanHandleReturnType(
        MemberInfo member,
        Type returnType,
        bool allowObjectType)
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
            return allowObjectType || HasConfiguration(member);
        }

        if (typeof(IAsyncResult).IsAssignableFrom(returnType))
        {
            if (returnType.IsGenericType)
            {
                var returnTypeDefinition = returnType.GetGenericTypeDefinition();

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

        if (returnType.IsByRefLike ||
            returnType.IsByRef)
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(returnType))
        {
            return HasConfiguration(member);
        }

        return true;
    }

    private static bool CanHandleParameter(ParameterInfo parameter, bool allowObjectType)
    {
        // schema, object type and object field can be injected into a resolver, so
        // we allow these as parameter type.
        var parameterType = parameter.ParameterType;

        if (typeof(ISchema).IsAssignableFrom(parameterType) ||
            typeof(IObjectType).IsAssignableFrom(parameterType) ||
            typeof(IOutputField).IsAssignableFrom(parameterType))
        {
            return true;
        }

        // All other types may cause errors and need to have an explicit configuration.
        if (parameterType == typeof(object))
        {
            return allowObjectType || HasConfiguration(parameter);
        }

        if (typeof(ITypeSystemMember).IsAssignableFrom(parameterType))
        {
            return HasConfiguration(parameter);
        }

        // Async results are not allowed.
        if (parameterType == typeof(ValueTask) ||
            parameterType == typeof(Task) ||
            typeof(IAsyncResult).IsAssignableFrom(parameterType))
        {
            return false;
        }

        if (parameterType.IsGenericType)
        {
            var parameterTypeDefinition = parameterType.GetGenericTypeDefinition();

            if (parameterTypeDefinition == typeof(ValueTask<>) ||
                parameterTypeDefinition == typeof(Task<>))
            {
                return false;
            }
        }

        // reflection types should also be excluded by default.
        if (typeof(ICustomAttributeProvider).IsAssignableFrom(parameterType))
        {
            return HasConfiguration(parameter);
        }

        // by ref and out will never be allowed
        if (parameterType.IsByRef ||
            parameter.ParameterType.IsByRefLike ||
            parameter.IsOut)
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(parameterType))
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
            element.IsDefined(typeof(ScopedStateAttribute), true) ||
            element.IsDefined(typeof(LocalStateAttribute), true) ||
            element.IsDefined(typeof(DescriptorAttribute), true);

    private static bool IsSystemMember(MemberInfo member)
    {
        if (member is MethodInfo m &&
            (m.Name.EqualsOrdinal(_toString) ||
                m.Name.EqualsOrdinal(_getHashCode) ||
                m.Name.EqualsOrdinal(_equals) ||
                m.Name.EqualsOrdinal(_compareTo) ||
                m.Name.EqualsOrdinal(_clone)))
        {
            return true;
        }

        return false;
    }

    private bool TryGetDefaultValueFromConstructor(
        PropertyInfo property,
        out object? defaultValue)
    {
        defaultValue = null;
        var constructors = property.DeclaringType!.GetConstructors();

        if (constructors.Length == 1)
        {
            foreach (var parameter in constructors[0].GetParameters())
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
