namespace Zeus.Abstractions
{
    /// <summary>
    /// Represent a GraphQL type definition.
    /// </summary>
    public interface ITypeDefinition
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>
        /// Name of the type.
        /// </returns>
        string Name { get; }

        /// <summary>
        /// Merges two type definitions of the same kind eg. two <see cref="ObjectTypeDefinition" />s
        /// and with the same type name.
        /// </summary>
        /// <param name="other">
        /// The other type definitions that shall 
        /// add or replace parts of this definition 
        /// with its own definition parts.
        /// </param>
        /// <returns>
        /// Returns the newly composed type that represents the merged type definition.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="other" /> type definition is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="other" /> type definition is of a different kind.
        /// - or  -
        /// The <paramref name="other" /> type definition has a different name.
        /// </exception>
        ITypeDefinition Merge(ITypeDefinition other);
    }
}