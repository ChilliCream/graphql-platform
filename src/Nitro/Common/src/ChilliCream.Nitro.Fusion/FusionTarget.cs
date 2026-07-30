namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// Identifies a Nitro API and the credentials used to access it.
/// </summary>
public sealed record FusionTarget(Uri CloudUrl, string ApiId, string ApiKey)
{
    /// <inheritdoc />
    public override string ToString() => $"{CloudUrl} (API {ApiId})";
}
