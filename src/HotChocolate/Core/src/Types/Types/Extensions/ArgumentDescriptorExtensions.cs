using HotChocolate.Language;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods to <see cref="IArgumentDescriptor"/>.
/// </summary>
public static class ArgumentDescriptorExtensions
{
    /// <summary>
    /// Specifies the default value of an input field with GraphQL syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The input field descriptor.
    /// </param>
    /// <param name="syntax">
    /// The GraphQL value syntax of the default value.
    /// </param>
    /// <returns>
    /// Returns the input field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// <paramref name="syntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL syntax is invalid.
    /// </exception>
    public static IArgumentDescriptor DefaultValueSyntax(
        this IArgumentDescriptor descriptor,
        [StringSyntax("graphql")] string syntax)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(syntax);

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(syntax);
        return descriptor.DefaultValue(value);
    }
}
