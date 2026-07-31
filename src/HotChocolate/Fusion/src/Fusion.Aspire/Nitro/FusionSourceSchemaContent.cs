namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Computes the canonical content identity of a Fusion source schema archive.
/// </summary>
internal static class FusionSourceSchemaContent
{
    /// <summary>
    /// Computes a SHA-256 over the normalized schema, settings, and extensions.
    /// </summary>
    public static async Task<string> ComputeSha256Async(
        string archivePath,
        string expectedName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedName);

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                "The Fusion source schema archive does not exist.",
                archivePath);
        }

        var content = await FusionArchiveContent.ReadAsync(
            archivePath,
            expectedName,
            cancellationToken);
        return content.ComputeSha256(cancellationToken);
    }

    /// <summary>
    /// Computes a SHA-256 over the normalized schema, settings, and extensions in memory.
    /// </summary>
    public static async Task<string> ComputeSha256Async(
        byte[] archive,
        string expectedName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedName);

        if (archive.Length is 0)
        {
            throw new ArgumentException(
                "The Fusion source schema archive is empty.",
                nameof(archive));
        }

        await using var stream = new MemoryStream(
            archive,
            writable: false);
        var content = await FusionArchiveContent.ReadAsync(
            stream,
            expectedName,
            cancellationToken);
        return content.ComputeSha256(cancellationToken);
    }
}
