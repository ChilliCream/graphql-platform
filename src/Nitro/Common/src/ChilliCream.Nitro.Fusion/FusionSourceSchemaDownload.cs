namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Owns an exact immutable source schema archive downloaded from Nitro together with its
/// canonical content identity.
/// </summary>
/// <remarks>
/// The caller must dispose this instance after consuming <see cref="Archive"/>. Disposal clears
/// the owned archive buffer.
/// </remarks>
public sealed class FusionSourceSchemaDownload : IDisposable
{
    private byte[]? _archive;

    /// <summary>
    /// Initializes a new owned source schema download.
    /// </summary>
    /// <remarks>
    /// Ownership of <paramref name="archive"/> transfers to this instance.
    /// </remarks>
    public FusionSourceSchemaDownload(
        string name,
        string version,
        byte[] archive,
        string contentSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentSha256);

        Name = name;
        Version = version;
        _archive = archive;
        ContentSha256 = contentSha256;
    }

    /// <summary>
    /// Gets the source schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the exact source schema version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets read-only access to the owned archive buffer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The download has already been disposed.
    /// </exception>
    public ReadOnlyMemory<byte> Archive
    {
        get
        {
            var archive = Volatile.Read(ref _archive);
            ObjectDisposedException.ThrowIf(archive is null, this);
            return archive;
        }
    }

    /// <summary>
    /// Gets the canonical source content digest.
    /// </summary>
    public string ContentSha256 { get; }

    /// <summary>
    /// Clears and releases the owned archive buffer.
    /// </summary>
    public void Dispose()
    {
        var archive = Interlocked.Exchange(ref _archive, null);
        if (archive is not null)
        {
            Array.Clear(archive);
        }
    }
}
