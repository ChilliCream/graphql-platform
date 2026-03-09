namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type.
/// </summary>
public interface IType : ITypeSystemMember, IEquatable<IType>
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    TypeKind Kind { get; }
}
