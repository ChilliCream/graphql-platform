using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The GraphQL naming conventions are used to format names and descriptions of types and members.
/// </summary>
public interface INamingConventions : IConvention
{
    /// <summary>
    /// Formats a type name to abide by the current type naming convention for GraphQL types.
    /// </summary>
    /// <param name="type">
    /// The runtime from which the name is derived.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    string GetTypeName(Type type);

    /// <summary>
    /// Formats a type name to abide by the current type naming convention for GraphQL types.
    /// </summary>
    /// <param name="type">
    /// The runtime from which the name is derived.
    /// </param>
    /// <param name="kind">
    /// The kind of GraphQL type the name is for.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    string GetTypeName(Type type, TypeKind kind);

    /// <summary>
    /// Gets the description of a GraphQL type from its runtime type.
    /// </summary>
    /// <param name="type">
    /// The runtime type from which the description is derived.
    /// </param>
    /// <param name="kind">
    /// The kind of GraphQL type the description is for.
    /// </param>
    /// <returns>
    /// Returns a description string that has the correct naming format.
    /// </returns>
    string? GetTypeDescription(Type type, TypeKind kind);

    /// <summary>
    /// Formats a member name to abide by the current member naming convention for GraphQL fields or arguments.
    /// </summary>
    /// <param name="member">
    /// The runtime member from which the name is derived.
    /// </param>
    /// <param name="kind">
    /// The kind of GraphQL member the name is for.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    string GetMemberName(MemberInfo member, MemberKind kind);

    /// <summary>
    /// Gets the description of a GraphQL field or argument from its runtime member.
    /// </summary>
    /// <param name="member">
    /// The runtime member from which the description is derived.
    /// </param>
    /// <param name="kind">
    /// The kind of GraphQL member the description is for.
    /// </param>
    /// <returns>
    /// Returns a description string that has the correct naming format.
    /// </returns>
    string? GetMemberDescription(MemberInfo member, MemberKind kind);

    /// <summary>
    /// Formats a parameter name to abide by the current naming convention for GraphQL arguments.
    /// </summary>
    /// <param name="parameter">
    /// The runtime parameter from which the name is derived.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    string GetArgumentName(ParameterInfo parameter);

    /// <summary>
    /// Gets the description of a GraphQL argument from its runtime parameter.
    /// </summary>
    /// <param name="parameter">
    /// The runtime parameter from which the description is derived.
    /// </param>
    /// <returns>
    /// Returns a description string that has the correct naming format.
    /// </returns>
    string? GetArgumentDescription(ParameterInfo parameter);

    /// <summary>
    /// Formats an enum value to abide by the current naming convention for GraphQL enum values.
    /// </summary>
    /// <param name="value">
    /// The enum value that needs formatting.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    string GetEnumValueName(object value);

    /// <summary>
    /// Gets the description of a GraphQL enum value from its runtime value.
    /// </summary>
    /// <param name="value">
    /// The runtime value from which the description is derived.
    /// </param>
    /// <returns>
    /// Returns a description string that has the correct naming format.
    /// </returns>
    string? GetEnumValueDescription(object value);

    /// <summary>
    /// Determines if a member is deprecated and returns the deprecation reason.
    /// </summary>
    /// <param name="member">
    /// The runtime member that needs to be checked for deprecation.
    /// </param>
    /// <param name="reason">
    /// The reason why the member is deprecated.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the member is deprecated, <c>false</c> otherwise.
    /// </returns>
    bool IsDeprecated(MemberInfo member, out string? reason);

    /// <summary>
    /// Determines if a value is deprecated and returns the deprecation reason.
    /// </summary>
    /// <param name="value">
    /// The value that needs to be checked for deprecation.
    /// </param>
    /// <param name="reason">
    /// The reason why the value is deprecated.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the value is deprecated, <c>false</c> otherwise.
    /// </returns>
    bool IsDeprecated(object value, out string? reason);

    /// <summary>
    /// Formats a fieldName to abide by the current field naming convention.
    /// </summary>
    /// <param name="fieldName">
    /// The field name that needs formatting.
    /// </param>
    /// <returns>
    /// Returns a name string that has the correct naming format.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The field is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    string FormatFieldName(string fieldName);
}
