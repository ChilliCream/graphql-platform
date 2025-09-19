namespace HotChocolate.Validation;

/// <summary>
/// Represents a pair of field infos.
/// </summary>
public readonly struct FieldInfoPair : IEquatable<FieldInfoPair>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldInfoPair"/>.
    /// </summary>
    /// <param name="fieldA">
    /// The first field info.
    /// </param>
    /// <param name="fieldB">
    /// The second field info.
    /// </param>
    public FieldInfoPair(FieldInfo fieldA, FieldInfo fieldB)
    {
        FieldA = fieldA;
        FieldB = fieldB;
    }

    /// <summary>
    /// Gets the first field info.
    /// </summary>
    public FieldInfo FieldA { get; }

    /// <summary>
    /// Gets the second field info.
    /// </summary>
    public FieldInfo FieldB { get; }

    /// <summary>
    /// Compares this field info pair to another field info pair.
    /// </summary>
    /// <param name="other">
    /// The other field info pair.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field info pairs are equal.
    /// </returns>
    public bool Equals(FieldInfoPair other)
        => FieldA.Equals(other.FieldA) && FieldB.Equals(other.FieldB);

    /// <summary>
    /// Compares this field info pair to another object.
    /// </summary>
    /// <param name="obj">
    /// The other object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field info pairs are equal.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is FieldInfoPair other && Equals(other);

    /// <summary>
    /// Returns the hash code for this field info pair.
    /// </summary>
    /// <returns>
    /// The hash code for this field info pair.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(FieldA, FieldB);
}
