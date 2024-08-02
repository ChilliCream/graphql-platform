using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL union type.
/// </summary>
public interface IUnionType : INamedOutputType
{
    /// <summary>
    /// Gets the <see cref="IObjectType" /> set of this union type.
    /// </summary>
    IReadOnlyCollection<IObjectType> Types { get; }

    /// <summary>
    /// Resolves the concrete type for the value of a type
    /// that implements this interface.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="resolverResult">
    /// The value for which the type shall be resolved.
    /// </param>
    /// <returns>
    /// Returns <c>null</c> if the value is not of a type
    /// implementing this interface.
    /// </returns>
    IObjectType? ResolveConcreteType(
        IResolverContext context,
        object resolverResult);

    /// <summary>
    /// Checks if the type set of this union type contains the
    /// specified <paramref name="objectType"/>.
    /// </summary>
    /// <param name="objectType">
    /// The object type.
    /// </param>
    /// <returns>
    /// Returns <c>true</c>, if the type set of this union type contains the
    /// specified <paramref name="objectType"/>; otherwise, <c>false</c> is returned.
    /// </returns>
    bool ContainsType(IObjectType objectType);

    /// <summary>
    /// Checks if the type set of this union type contains the
    /// specified <paramref name="typeName"/>.
    /// </summary>
    /// <param name="typeName">
    /// The type name.
    /// </param>
    /// <returns>
    /// Returns <c>true</c>, if the type set of this union type contains the
    /// specified <paramref name="typeName"/>; otherwise, <c>false</c> is returned.
    /// </returns>
    bool ContainsType(string typeName);
}
