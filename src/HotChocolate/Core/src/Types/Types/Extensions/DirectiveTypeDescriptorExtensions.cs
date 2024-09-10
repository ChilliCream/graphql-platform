using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

public static class DirectiveTypeDescriptorExtensions
{
    public static IDirectiveTypeDescriptor<T> Ignore<T>(
        this IDirectiveTypeDescriptor<T> descriptor,
        Expression<Func<T, object>> property)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        descriptor.Argument(property).Ignore();
        return descriptor;
    }

    /// <summary>
    /// Specifies the type of a directive argument with GraphQL SDL type syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The directive argument descriptor.
    /// </param>
    /// <param name="typeSyntax">
    /// The GraphQL SDL type syntax.
    /// </param>
    /// <returns>
    /// Returns the directive argument descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL SDL type syntax is invalid.
    /// </exception>
    public static IDirectiveArgumentDescriptor Type(
        this IDirectiveArgumentDescriptor descriptor,
        string typeSyntax)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (typeSyntax is null)
        {
            throw new ArgumentNullException(nameof(typeSyntax));
        }

        return descriptor.Type(Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax));
    }
}
