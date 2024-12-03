using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

/// <summary>
/// Specifies the GraphQL type.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property |
    AttributeTargets.Method |
    AttributeTargets.Parameter)]
public class GraphQLTypeAttribute : Attribute
{
    /// <summary>
    /// Specifies the GraphQL type.
    /// </summary>
    /// <param name="type">The GraphQL type.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public GraphQLTypeAttribute(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Specifies the GraphQL type with SDL type syntax e.g. `[String!]!`.
    /// </summary>
    /// <param name="typeSyntax">A string representing a GraphQL type.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    public GraphQLTypeAttribute(string typeSyntax)
    {
        if (typeSyntax is null)
        {
            throw new ArgumentNullException(nameof(typeSyntax));
        }

        TypeSyntax = Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax);
    }

    /// <summary>
    /// Gets the GraphQL Type.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Gets the GraphQL type syntax.
    /// </summary>
    public ITypeNode? TypeSyntax { get; }
}

/// <summary>
/// Specifies the GraphQL type.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property |
    AttributeTargets.Method |
    AttributeTargets.Parameter)]
public sealed class GraphQLTypeAttribute<T> : GraphQLTypeAttribute where T : IType
{
    public GraphQLTypeAttribute() : base(typeof(T))
    {
    }
}
