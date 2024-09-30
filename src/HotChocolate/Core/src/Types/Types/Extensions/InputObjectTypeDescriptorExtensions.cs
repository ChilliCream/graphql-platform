using System.Linq.Expressions;
using HotChocolate.Language;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods to <see cref="IInputObjectTypeDescriptor"/>.
/// </summary>
public static class InputObjectTypeDescriptorExtensions
{
    /// <summary>
    /// Ignores the specified <paramref name="property"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The input type descriptor.
    /// </param>
    /// <param name="property">
    /// An expression representing the property that shall be ignored.
    /// </param>
    /// <typeparam name="T">
    /// The runtime type of the input object.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c> or
    /// the <paramref name="property"/> is <c>null</c>.
    /// </exception>
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
    /// Defines an input object type as a oneof input object type
    /// where only ever one field can hold a value.
    /// </summary>
    /// <param name="descriptor">
    /// The input type descriptor.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInputObjectTypeDescriptor OneOf(this IInputObjectTypeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownDirectives.OneOf);
    }

    /// <summary>
    /// Defines an input object type as a oneof input object type
    /// where only ever one field can hold a value.
    /// </summary>
    /// <param name="descriptor">
    /// The input type descriptor.
    /// </param>
    /// <typeparam name="T">
    /// The runtime type of the input object.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInputObjectTypeDescriptor<T> OneOf<T>(
        this IInputObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(WellKnownDirectives.OneOf);
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
    public static IInputFieldDescriptor DefaultValueSyntax(
        this IInputFieldDescriptor descriptor,
        [StringSyntax("graphql")] string syntax)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (syntax is null)
        {
            throw new ArgumentNullException(nameof(syntax));
        }

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(syntax);
        return descriptor.DefaultValue(value);
    }
}
