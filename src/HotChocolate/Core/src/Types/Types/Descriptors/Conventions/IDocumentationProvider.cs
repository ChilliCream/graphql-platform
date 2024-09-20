using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The documentation provider is able to extract GraphQL type system
/// documentation from the associated .NET type system.
/// </summary>
public interface IDocumentationProvider
{
    /// <summary>
    /// Gets the documentation for a GraphQL type from the
    /// associated <see cref="Type" />.
    /// </summary>
    /// <param name="type">
    /// The type from which the documentation shall be extracted.
    /// </param>
    /// <returns>
    /// Returns a markdown string (https://commonmark.org)
    /// describing the GraphQL type.
    /// </returns>
    string? GetDescription(Type type);

    /// <summary>
    /// Gets the documentation for a GraphQL input-, output-field or
    /// directive argument from the associated <see cref="MemberInfo" />.
    /// </summary>
    /// <param name="member">
    /// The member from which the documentation shall be extracted.
    /// </param>
    /// <returns>
    /// Returns a markdown string (https://commonmark.org)
    /// describing the GraphQL input-, output-field or
    /// directive argument,
    /// </returns>
    string? GetDescription(MemberInfo member);

    /// <summary>
    /// Gets the documentation for a GraphQL field argument from the
    /// associated <see cref="ParameterInfo" />.
    /// </summary>
    /// <param name="parameter">
    /// The parameter from which the documentation shall be extracted.
    /// </param>
    /// <returns>
    /// Returns a markdown string (https://commonmark.org)
    /// describing the GraphQL field argument.
    /// </returns>
    string? GetDescription(ParameterInfo parameter);
}
