using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

public static class InterfaceTypeDescriptorExtensions
{
    public static IInterfaceTypeDescriptor<T> Ignore<T>(
        this IInterfaceTypeDescriptor<T> descriptor,
        Expression<Func<T, object?>> propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);
        descriptor.Field(propertyOrMethod).Ignore();
        return descriptor;
    }

    /// <summary>
    /// Specifies the type of an interface field with GraphQL SDL type syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The interface field descriptor.
    /// </param>
    /// <param name="typeSyntax">
    /// The GraphQL SDL type syntax.
    /// </param>
    /// <returns>
    /// Returns the interface field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL SDL type syntax is invalid.
    /// </exception>
    public static IInterfaceFieldDescriptor Type(
        this IInterfaceFieldDescriptor descriptor,
        string typeSyntax)
    {
        ArgumentNullException.ThrowIfNull(typeSyntax);
        return descriptor.Type(Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax));
    }
}
