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

    public async Task<object> ToObject(string[] formats)
    {
        return new
        {
            _data.Id,
            _data.Name,
            Api = _data.Api is { } api
                ? new { api.Name }
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

    private async Task<object?> RenderVersions()
    {
        var versions = await _clientNodes.Value;
        return versions.Select(x => new { x.Tag, x.CreatedAt, x.Stages });
    }

    private async Task<object?> RenderPublishedVersions()
    {
        var versions = await _clientNodes.Value;

        return versions
            .SelectMany(x => x.Stages.Select(y => (Stage: y, x.Tag)))
            .GroupBy(x => x.Stage)
            .Select(x => new { Stage = x.Key, Versions = x.Select(y => y.Tag).ToArray() })
            .ToArray();
    }

    public static ClientDetailPrompt From(IClientDetailPrompt_Client data, IApiClient client)
        => new(data, client);

    private record struct VersionInfo(
        string Tag,
        DateTimeOffset CreatedAt,
        string[] Stages);
}
