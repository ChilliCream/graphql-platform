using HotChocolate.Types;

namespace HotChocolate.Internal
{
    /// <summary>
    /// Represents a GraphQL type factory.
    /// </summary>
    public interface ITypeFactory
    {
        /// <summary>
        /// Creates a type structure with the <paramref name="namedType"/>.
        /// </summary>
        /// <param name="namedType">The named type component.</param>
        /// <returns>
        /// Returns a GraphQL type structure.
        /// </returns>
        IType CreateType(INamedType namedType);
    }
}
