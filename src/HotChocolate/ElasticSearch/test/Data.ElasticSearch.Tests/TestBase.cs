using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;
using Squadron;

namespace HotChocolate.Data.ElasticSearch;

public class TestBase
{
    protected TestBase(ElasticsearchResource resource)
    {
        Uri uri = new($"http://{resource.Instance.Address}:{resource.Instance.HostPort}");
        Resource = resource;
        Client = new ElasticClient(new ConnectionSettings(uri)
            .EnableDebugMode()
            .DisableDirectStreaming()
            .DefaultIndex(DefaultIndexName));
    }

    protected ElasticsearchResource Resource { get; }

    protected ElasticClient Client { get; }

    protected string DefaultIndexName { get; } = $"{Guid.NewGuid():N}";

    protected async Task SetupMapping<T>()
        where T : class
    {
        await Client.Indices
            .CreateAsync(DefaultIndexName, c => c.Map(x => x.AutoMap<T>()));
    }

    protected async Task ClearData()
    {
        await Client.Indices.DeleteAsync(Indices.AllIndices);
    }

    protected async Task IndexDocuments<T>(IEnumerable<T> data)
        where T : class
    {
        await ClearData();
        await SetupMapping<T>();
        foreach (T element in data)
        {
            await Client.IndexDocumentAsync(element);
        }

        await Client.Indices.RefreshAsync(Indices.AllIndices);
    }
}
