using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL object type
/// </summary>
public interface IObjectType : IComplexOutputType
{
    /// <summary>
    /// Gets the field that the type exposes.
    /// </summary>
    new IFieldCollection<IObjectField> Fields { get; }

    /// <summary>
    /// Specifies if the specified <paramref name="resolverResult" /> is an instance of
    /// this object type.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="resolverResult">
    /// The result that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <paramref name="context"/> is an instance of this type;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsInstanceOfType(IResolverContext context, object resolverResult);
}
