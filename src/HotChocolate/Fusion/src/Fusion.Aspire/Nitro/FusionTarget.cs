namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Identifies a Nitro API and the credentials used to access it.
/// </summary>
internal sealed record FusionTarget(Uri CloudUrl, string ApiId, string ApiKey)
{
    /// <inheritdoc />
    public override string ToString() => $"{CloudUrl} (API {ApiId})";
}
