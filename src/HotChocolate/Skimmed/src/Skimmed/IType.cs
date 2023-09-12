namespace HotChocolate.Skimmed;

public interface IType : IEquatable<IType>
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    TypeKind Kind { get; }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <param name="comparison">
    /// Specifies the comparison type.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, <see langword="false" />.
    /// </returns>
    bool Equals(IType? other, TypeComparison comparison);
}