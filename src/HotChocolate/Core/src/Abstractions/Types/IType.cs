namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL type.
    /// </summary>
    public interface IType : ITypeSystemMember
    {
        /// <summary>
        /// Gets the type kind.
        /// </summary>
        TypeKind Kind { get; }
    }
}
