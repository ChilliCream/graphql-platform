namespace StrawberryShake;

/// <summary>
/// Represents a session that allows to apply changes to the entity
/// store or read from the store.
/// </summary>
public interface IEntityUpdateSession : IDisposable
{
    /// <summary>
    /// Gets the store version.
    /// </summary>
    public ulong Version { get; }
}

/// <summary>
/// Represents a session that allows to apply changes to the entity
/// store or read from the store.
/// </summary>
public interface IEntityReadSession : IDisposable
{
    /// <summary>
    /// Gets the store version.
    /// </summary>
    public ulong Version { get; }
}
