using HotChocolate.Language;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL interface type
    /// </summary>
    public interface IInterfaceType : IComplexOutputType
    {
        /// <summary>
        /// Gets the syntax node representation of this interface type.
        /// </summary>
        new InterfaceTypeDefinitionNode? SyntaxNode { get; }

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
    }
}
