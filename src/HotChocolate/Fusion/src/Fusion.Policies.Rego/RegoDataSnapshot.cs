namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// An immutable data document produced by an <see cref="IRegoDataProvider"/>.
/// </summary>
/// <remarks>
/// The snapshot owns a private copy of the UTF-8 encoded JSON it was constructed with; it never
/// retains memory owned by the provider. Two snapshots are equal when their <see cref="Version"/>
/// is equal, regardless of their data.
/// </remarks>
public sealed class RegoDataSnapshot : IEquatable<RegoDataSnapshot>
{
    private readonly byte[] _data;

    /// <summary>
    /// Initializes a new instance of <see cref="RegoDataSnapshot"/>, copying <paramref name="data"/>
    /// into memory the snapshot owns.
    /// </summary>
    /// <param name="data">The UTF-8 encoded JSON object the provider currently publishes.</param>
    /// <param name="version">An opaque token that changes whenever the provider's data changes.</param>
    public RegoDataSnapshot(ReadOnlySpan<byte> data, string version)
    {
        ArgumentException.ThrowIfNullOrEmpty(version);

        _data = data.ToArray();
        Version = version;
    }

    /// <summary>
    /// Gets the UTF-8 encoded JSON object this snapshot carries.
    /// </summary>
    public ReadOnlyMemory<byte> Data => _data;

    /// <summary>
    /// Gets the opaque version token that identifies this snapshot's content.
    /// </summary>
    public string Version { get; }

    /// <inheritdoc />
    public bool Equals(RegoDataSnapshot? other)
        => other is not null && string.Equals(Version, other.Version, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as RegoDataSnapshot);

    /// <inheritdoc />
    public override int GetHashCode() => Version.GetHashCode(StringComparison.Ordinal);
}
