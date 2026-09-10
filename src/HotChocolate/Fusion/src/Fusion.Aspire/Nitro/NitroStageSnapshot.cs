using System.Security.Cryptography;
using System.Text;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal enum NitroStageChangeKind
{
    FusionConfigurationPublished,
    ClientVersionPublished,
    ClientVersionUnpublished,
    ClientDeleted
}

internal sealed record NitroStageChange(
    NitroStageChangeKind Kind,
    string? FusionConfigurationId = null,
    string? ClientId = null,
    string? ClientVersionId = null);

internal sealed class NitroStageSnapshot
{
    private readonly Dictionary<string, HashSet<string>> _clientVersions;

    public NitroStageSnapshot(
        string? fusionConfigurationId,
        Dictionary<string, HashSet<string>> clientVersions)
    {
        FusionConfigurationId = fusionConfigurationId;
        _clientVersions = clientVersions;
        Identity = CreateIdentity(fusionConfigurationId, clientVersions);
    }

    public string? FusionConfigurationId { get; }

    public string Identity { get; }

    public NitroStageSnapshot Apply(NitroStageChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var fusionConfigurationId = FusionConfigurationId;
        var clientVersions = CloneClientVersions();

        switch (change.Kind)
        {
            case NitroStageChangeKind.FusionConfigurationPublished:
                fusionConfigurationId = change.FusionConfigurationId;
                break;

            case NitroStageChangeKind.ClientVersionPublished:
                if (change.ClientId is { } publishedClientId
                    && change.ClientVersionId is { } publishedVersionId)
                {
                    if (!clientVersions.TryGetValue(publishedClientId, out var versions))
                    {
                        versions = new HashSet<string>(StringComparer.Ordinal);
                        clientVersions.Add(publishedClientId, versions);
                    }

                    versions.Add(publishedVersionId);
                }
                break;

            case NitroStageChangeKind.ClientVersionUnpublished:
                if (change.ClientId is { } unpublishedClientId
                    && change.ClientVersionId is { } unpublishedVersionId
                    && clientVersions.TryGetValue(unpublishedClientId, out var publishedVersions))
                {
                    publishedVersions.Remove(unpublishedVersionId);
                    if (publishedVersions.Count == 0)
                    {
                        clientVersions.Remove(unpublishedClientId);
                    }
                }
                break;

            case NitroStageChangeKind.ClientDeleted:
                if (change.ClientId is { } deletedClientId)
                {
                    clientVersions.Remove(deletedClientId);
                }
                break;
        }

        return new NitroStageSnapshot(fusionConfigurationId, clientVersions);
    }

    private Dictionary<string, HashSet<string>> CloneClientVersions()
        => _clientVersions.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static string CreateIdentity(
        string? fusionConfigurationId,
        Dictionary<string, HashSet<string>> clientVersions)
    {
        var builder = new StringBuilder();
        builder.Append("fusion:").Append(fusionConfigurationId).Append('\n');

        foreach (var (clientId, versions) in clientVersions.OrderBy(
            pair => pair.Key,
            StringComparer.Ordinal))
        {
            builder.Append("client:").Append(clientId).Append(':');
            foreach (var versionId in versions.Order(StringComparer.Ordinal))
            {
                builder.Append(versionId).Append(',');
            }

            builder.Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
