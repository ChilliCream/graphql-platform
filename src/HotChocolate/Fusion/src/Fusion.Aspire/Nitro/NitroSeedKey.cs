using System.Security.Cryptography;
using System.Text;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Identifies a cached fusion configuration. A fusion configuration is identified by the Nitro
/// API it was downloaded from, the api it belongs to, and the stage it was downloaded for.
/// </summary>
internal sealed class NitroSeedKey
{
    private const int HashByteLength = 16;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroSeedKey"/>.
    /// </summary>
    /// <param name="apiUrl">
    /// The normalized Nitro API base URL.
    /// </param>
    /// <param name="apiId">
    /// The id of the api that carries the fusion configuration.
    /// </param>
    /// <param name="stage">
    /// The name of the stage the fusion configuration was downloaded for.
    /// </param>
    public NitroSeedKey(Uri apiUrl, string apiId, string stage)
    {
        ArgumentNullException.ThrowIfNull(apiUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        ApiUrl = apiUrl;
        ApiId = apiId;
        Stage = stage;
        Hash = ComputeHash(apiUrl, apiId, stage);
    }

    /// <summary>
    /// Gets the normalized Nitro API base URL.
    /// </summary>
    public Uri ApiUrl { get; }

    /// <summary>
    /// Gets the id of the api that carries the fusion configuration.
    /// </summary>
    public string ApiId { get; }

    /// <summary>
    /// Gets the name of the stage the fusion configuration was downloaded for.
    /// </summary>
    public string Stage { get; }

    /// <summary>
    /// Gets the file-name-safe hash of the identifying values.
    /// </summary>
    public string Hash { get; }

    private static string ComputeHash(Uri apiUrl, string apiId, string stage)
    {
        var value = $"{apiUrl.AbsoluteUri}\n{apiId}\n{stage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexStringLower(hash.AsSpan(0, HashByteLength));
    }
}
