namespace StrawberryShake;

/// <summary>
/// Represents the internal entity id which is used to interact with the client store.
/// </summary>
public readonly struct EntityId
{
    /// <summary>
    /// Initializes a new instance of <see cref="EntityId"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL type name.
    /// </param>
    /// <param name="value">
    /// The internal ID value.
    /// </param>
    public EntityId(string name, object value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Deconstruct(out string name, out object value)
    {
        name = Name;
        value = Value;
    }

    /// <summary>
    /// Gets the GraphQL type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the internal ID value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Indicates whether this instance and a specified <paramref name="other"/> are equal.
    /// </summary>
    /// <param name="other">
    /// The other <see cref="EntityId"/> to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> and this instance are
    /// the same type and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(EntityId other) =>
        Name == other.Name && Value.Equals(other.Value);

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are
    /// the same type and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is EntityId other && Equals(other);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return Name.GetHashCode() * 397 ^
                Value.GetHashCode() * 397;
        }
    }

    public static bool operator ==(EntityId x, EntityId y) =>
        x.Equals(y);

    public static bool operator !=(EntityId x, EntityId y) =>
        !x.Equals(y);
}
