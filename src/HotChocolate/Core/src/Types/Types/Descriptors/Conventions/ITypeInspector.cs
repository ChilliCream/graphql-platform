using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The type inspector provides helpers to inspect .NET types and
/// infer GraphQL type structures.
/// </summary>
public interface ITypeInspector : IConvention
{
    /// <summary>
    /// Gets the relevant members of a object or input object.
    /// </summary>
    /// <param name="type">
    /// The type that represents the object type.
    /// </param>
    /// <param name="includeIgnored">
    /// Specifies if also by default ignored members shall be returned.
    /// </param>
    /// <param name="includeStatic">
    /// Specifies if static members shall be returned.
    /// </param>
    /// <param name="allowObject">
    /// Specifies if object is allowed as parameter or return type without a type attribute.
    /// </param>
    /// <returns>
    /// Returns the relevant members of a object or input object.
    /// </returns>
    ReadOnlySpan<MemberInfo> GetMembers(
        Type type,
        bool includeIgnored = false,
        bool includeStatic = false,
        bool allowObject = false);

    /// <summary>
    /// Defines if a member shall be ignored. This method interprets ignore attributes.
    /// </summary>
    /// <param name="member">
    /// The member that shall be inspected.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member shall be ignored; otherwise, <c>false</c>.
    /// </returns>
    bool IsMemberIgnored(MemberInfo member);

    /// <summary>
    /// Gets the field type reference from a <see cref="MemberInfo" />.
    /// </summary>
    /// <param name="member">
    /// The member from which the field type shall be extracted.
    /// </param>
    /// <param name="context">
    /// The context defines if the field has an input or output context.
    /// </param>
    /// <param name="scope">
    /// The type reference scope.
    /// </param>
    /// <param name="ignoreAttributes">
    /// Ignores the attributes applied to the member e.g. <see cref="GraphQLTypeAttribute"/>.
    /// </param>
    /// <returns>
    /// Returns a type reference describing the type of the field.
    /// </returns>
    TypeReference GetReturnTypeRef(
        MemberInfo member,
        TypeContext context = TypeContext.None,
        string? scope = null,
        bool ignoreAttributes = false);

    /// <summary>
    /// Gets the field type from a <see cref="MemberInfo" />.
    /// </summary>
    /// <param name="member">
    /// The member from which the field type shall be extracted.
    /// </param>
    /// <param name="ignoreAttributes">
    /// Ignores the attributes applied to the member e.g. <see cref="GraphQLTypeAttribute"/>.
    /// </param>
    /// <returns>
    /// Returns a type reference describing the type of the field.
    /// </returns>
    IExtendedType GetReturnType(MemberInfo member, bool ignoreAttributes = false);

    /// <summary>
    /// Gets the field argument type reference from a <see cref="ParameterInfo" />.
    /// </summary>
    /// <param name="parameter">
    /// The parameter from which the argument type shall be extracted.
    /// </param>
    /// <param name="scope">
    /// The type reference scope.
    /// </param>
    /// <param name="ignoreAttributes">
    /// Ignores the attributes applied to the member e.g. <see cref="GraphQLTypeAttribute"/>.
    /// </param>
    /// <returns>
    /// Returns a type reference describing the type of the argument.
    /// </returns>
    TypeReference GetArgumentTypeRef(
        ParameterInfo parameter,
        string? scope = null,
        bool ignoreAttributes = false);

    /// <summary>
    /// Gets the field argument type from a <see cref="ParameterInfo" />.
    /// </summary>
    /// <param name="parameter">
    /// The parameter from which the argument type shall be extracted.
    /// </param>
    /// <param name="ignoreAttributes">
    /// Ignores the attributes applied to the member e.g. <see cref="GraphQLTypeAttribute"/>.
    /// </param>
    /// <returns>
    /// Returns a type reference describing the type of the argument.
    /// </returns>
    IExtendedType GetArgumentType(
        ParameterInfo parameter,
        bool ignoreAttributes = false);

    /// <summary>
    /// Gets a type reference from a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="context">
    /// The context defines if the field has an input or output context.
    /// </param>
    /// <param name="scope">
    /// The type scope.
    /// </param>
    /// <returns></returns>
    ExtendedTypeReference GetTypeRef(
        Type type,
        TypeContext context = TypeContext.None,
        string? scope = null);

    /// <summary>
    /// Gets the extended type representation for the provided <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns the extended type representation for the provided <paramref name="type"/>.
    /// </returns>
    IExtendedType GetType(Type type);

    /// <summary>
    /// Gets the extended type representation for the provided <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="nullable">
    /// Defines an array that specifies how to apply nullability information
    /// to the type components.
    /// </param>
    /// <returns>
    /// Returns the extended type representation for the provided <paramref name="type"/>.
    /// </returns>
    IExtendedType GetType(Type type, params bool?[] nullable);

    /// <summary>
    /// Gets the extended type representation for the provided <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="nullable">
    /// Defines an array that specifies how to apply nullability information
    /// to the type components.
    /// </param>
    /// <returns>
    /// Returns the extended type representation for the provided <paramref name="type"/>.
    /// </returns>
    IExtendedType GetType(Type type, ReadOnlySpan<bool?> nullable);

    /// <summary>
    /// Extracts the values of an enum type.
    /// </summary>
    /// <param name="enumType">
    /// The enum type.
    /// </param>
    /// <returns>
    /// Returns the extracted values of an enum type.
    /// </returns>
    IEnumerable<object> GetEnumValues(Type enumType);

    /// <summary>
    /// Gets the associated enum value member.
    /// </summary>
    /// <param name="value">
    /// The enum value.
    /// </param>
    /// <returns>
    /// Returns the associated enum value member.
    /// </returns>
    MemberInfo? GetEnumValueMember(object value);

    /// <summary>
    /// Gets the member that represents the node ID.
    /// </summary>
    /// <param name="type">
    /// The type from which the node ID shall be inferred.
    /// </param>
    /// <returns>
    /// The member that represents node ID or <c>null</c>.
    /// </returns>
    MemberInfo? GetNodeIdMember(Type type);

    /// <summary>
    /// Gets the method that represents the node resolver.
    /// </summary>
    /// <param name="nodeType">
    /// The type which represents a node.
    /// </param>
    /// <param name="resolverType">
    /// The type which provides a resolver to load a node by its id.
    /// </param>
    /// <returns>
    /// The member that represents node resolver or <c>null</c>.
    /// </returns>
    MethodInfo? GetNodeResolverMethod(Type nodeType, Type? resolverType = null);

    /// <summary>
    /// Extracts the named type from a type structure.
    /// </summary>
    /// <param name="type">The original type structure.</param>
    /// <returns>
    /// Returns the named type form a type structure.
    /// </returns>
    Type ExtractNamedType(Type type);

    /// <summary>
    /// Checks if the provided type is a schema type.
    /// </summary>
    /// <param name="type">
    /// The system type that shall be evaluated.
    /// </param>
    /// <returns>
    /// <c>true</c> if the provided type is a schema type.
    /// </returns>
    bool IsSchemaType(Type type);

    /// <summary>
    /// Applies the attribute configurations to the descriptor.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="descriptor">
    /// The descriptor to which the configuration shall be applied to.
    /// </param>
    /// <param name="attributeProvider">
    /// The attribute provider.
    /// </param>
    void ApplyAttributes(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider attributeProvider);

    /// <summary>
    /// Tries to extract a default value from a parameter.
    /// </summary>
    /// <param name="parameter">
    /// The parameter from which the default value shall be extracted.
    /// </param>
    /// <param name="defaultValue">
    /// The default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if a default value was available.
    /// </returns>
    bool TryGetDefaultValue(
        ParameterInfo parameter,
        out object? defaultValue);

    /// <summary>
    /// Tries to extract a default value from a property.
    /// </summary>
    /// <param name="property">
    /// The property from which the default value shall be extracted.
    /// </param>
    /// <param name="defaultValue">
    /// The default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if a default value was available.
    /// </returns>
    bool TryGetDefaultValue(
        PropertyInfo property,
        out object? defaultValue);

    /// <summary>
    /// Rewrites a types nullability.
    /// </summary>
    /// <param name="type">
    /// The original type.
    /// </param>
    /// <param name="nullable">
    /// The new nullability pattern.
    /// </param>
    /// <returns>
    /// Returns a new type that conforms to the new nullability pattern.
    /// </returns>
    IExtendedType ChangeNullability(IExtendedType type, params bool?[] nullable);

    /// <summary>
    /// Rewrites a types nullability.
    /// </summary>
    /// <param name="type">
    /// The original type.
    /// </param>
    /// <param name="nullable">
    /// The new nullability pattern.
    /// </param>
    /// <returns>
    /// Returns a new type that conforms to the new nullability pattern.
    /// </returns>
    IExtendedType ChangeNullability(IExtendedType type, ReadOnlySpan<bool?> nullable);

    /// <summary>
    /// Collects the nullability information from the given type.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <returns>
    /// Returns the nullability from the type.
    /// </returns>
    bool?[] CollectNullability(IExtendedType type);

    /// <summary>
    /// Collects the nullability information from the given type.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="buffer">
    /// The buffer to which the nullability status is written to.
    /// </param>
    /// <param name="written">
    /// Specifies how many nullability information was written to the buffer.
    /// </param>
    /// <returns>
    /// <c>true</c> if the buffer had sufficient space.
    /// </returns>
    bool CollectNullability(IExtendedType type, Span<bool?> buffer, out int written);

    /// <summary>
    /// Create a <see cref="ITypeInfo"/> from the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The system type from which the <see cref="ITypeInfo"/> shall be created.
    /// </param>
    /// <returns>
    /// The type info.
    /// </returns>
    ITypeInfo CreateTypeInfo(Type type);

    /// <summary>
    /// Create a <see cref="ITypeInfo"/> from the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The system type from which the <see cref="ITypeInfo"/> shall be created.
    /// </param>
    /// <returns>
    /// The type info.
    /// </returns>
    ITypeInfo CreateTypeInfo(IExtendedType type);

    /// <summary>
    /// Create a <see cref="ITypeFactory"/> from the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The system type from which the <see cref="ITypeFactory"/> shall be created.
    /// </param>
    /// <returns>
    /// The type factory.
    /// </returns>
    ITypeFactory CreateTypeFactory(IExtendedType type);

    /// <summary>
    /// Tries to create a <see cref="ITypeInfo"/> from the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The system type from which the <see cref="ITypeInfo"/> shall be created.
    /// </param>
    /// <param name="typeInfo">
    /// The type info.
    /// </param>
    /// <returns>
    /// <c>true</c> if a type info could be created.
    /// </returns>
    bool TryCreateTypeInfo(
        Type type,
        [NotNullWhen(true)] out ITypeInfo? typeInfo);

    /// <summary>
    /// Tries to create a <see cref="ITypeInfo"/> from the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The extended type from which the <see cref="ITypeInfo"/> shall be created.
    /// </param>
    /// <param name="typeInfo">
    /// The type info.
    /// </param>
    /// <returns>
    /// <c>true</c> if a type info could be created.
    /// </returns>
    bool TryCreateTypeInfo(
        IExtendedType type,
        [NotNullWhen(true)] out ITypeInfo? typeInfo);
}
