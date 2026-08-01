namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Identifies a Nitro API and the credential used to access it.
/// </summary>
internal sealed record FusionTarget(
    Uri CloudUrl,
    string ApiId,
    NitroCredential Credential)
{
    public FusionTarget(Uri cloudUrl, string apiId, string apiKey)
        : this(cloudUrl, apiId, NitroCredential.FromApiKey(apiKey))
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"{CloudUrl} (API {ApiId})";
}
