using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using static ChilliCream.Nitro.CommandLine.Cloud.ClientDetailFields;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal sealed class ClientDetailPrompt
{
    private readonly IClientDetailPrompt_Client _data;
    private readonly IApiClient _client;
    private readonly Lazy<Task<IReadOnlyList<VersionInfo>>> _clientNodes;

    private ClientDetailPrompt(IClientDetailPrompt_Client data, IApiClient client)
    {
        _data = data;
        _clientNodes = new Lazy<Task<IReadOnlyList<VersionInfo>>>(FetchVersions);
        _client = client;
    }

    public async Task<ClientDetailPromptResult> ToObject(string[] formats)
    {
        return new ClientDetailPromptResult
        {
            Id = _data.Id,
            Name = _data.Name,
            Api = _data.Api is { } api
                ? new Api { Name = api.Name }
                : null,
            Versions = formats.Contains(Versions)
                ? await RenderVersions()
                : null,
            PublishedVersions = formats.Contains(PublishedVersions)
                ? await RenderPublishedVersions()
                : null
        };
    }

    private async Task<IReadOnlyList<VersionInfo>> FetchVersions()
    {
        var versions = _data.Versions?.Edges?
            .Select(x => new VersionInfo(
                x.Node.Tag,
                x.Node.CreatedAt,
                x.Node.PublishedTo.Select(y => y.Stage?.Name ?? "-").ToArray()))
            .ToList() ?? [];
        if (_data.Versions?.PageInfo.HasNextPage is true)
        {
            var nextCursor = _data.Versions.PageInfo.EndCursor;
            while (nextCursor is not null)
            {
                var next =
                    await _client.PageClientVersionDetailQuery.ExecuteAsync(_data.Id, nextCursor);
                var client = (IPageClientVersionDetailQuery_Node_Client)next.EnsureData().Node!;

                var nextVersions = client.Versions?.Edges?.Select(x =>
                    new VersionInfo(
                        x.Node.Tag,
                        x.Node.CreatedAt,
                        x.Node.PublishedTo.Select(y => y.Stage?.Name ?? "-").ToArray())) ?? [];

                versions.AddRange(nextVersions);
                nextCursor =
                    client.Versions?.PageInfo.HasNextPage is true
                        ? client.Versions?.PageInfo.EndCursor
                        : null;
            }
        }

        return versions;
    }

    private async Task<IReadOnlyList<VersionInfo>> RenderVersions()
    {
        var versions = await _clientNodes.Value;
        return versions.ToList();
    }

    private async Task<IReadOnlyList<PublishedVersion>> RenderPublishedVersions()
    {
        var versions = await _clientNodes.Value;

        return versions
            .SelectMany(x => x.Stages.Select(y => (Stage: y, x.Tag)))
            .GroupBy(x => x.Stage)
            .Select(x => new PublishedVersion { Stage = x.Key, Versions = x.Select(y => y.Tag).ToList() })
            .ToList();
    }

    public static ClientDetailPrompt From(IClientDetailPrompt_Client data, IApiClient client)
        => new(data, client);

    public class ClientDetailPromptResult
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required Api? Api { get; init; }

        public required IReadOnlyList<VersionInfo>? Versions { get; init; }

        public required IReadOnlyList<PublishedVersion>? PublishedVersions { get; init; }
    }

    public record VersionInfo(
        string Tag,
        DateTimeOffset CreatedAt,
        string[] Stages);

    public class PublishedVersion
    {
        public required string Stage { get; init; }

        public required IReadOnlyList<string> Versions { get; init; }
    }

    public class Api
    {
        public required string Name { get; init; }
    }
}
