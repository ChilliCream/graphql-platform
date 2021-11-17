using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

public static class InputObjectTypeDescriptorExtensions
{
    public static IInputObjectTypeDescriptor<T> Ignore<T>(
        this IInputObjectTypeDescriptor<T> descriptor,
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

        descriptor.Field(property).Ignore();
        return descriptor;
    }

    /// <summary>
    /// Specifies the type of an input field with GraphQL SDL type syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The input field descriptor.
    /// </param>
    /// <param name="typeSyntax">
    /// The GraphQL SDL type syntax.
    /// </param>
    /// <returns>
    /// Returns the input field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL SDL type syntax is invalid.
    /// </exception>
    public static IInputFieldDescriptor Type(
        this IInputFieldDescriptor descriptor,
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
