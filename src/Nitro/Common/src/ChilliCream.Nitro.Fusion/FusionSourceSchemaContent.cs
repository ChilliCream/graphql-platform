namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Computes the canonical content identity of a Fusion source schema archive.
/// </summary>
public static class FusionSourceSchemaContent
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
}
