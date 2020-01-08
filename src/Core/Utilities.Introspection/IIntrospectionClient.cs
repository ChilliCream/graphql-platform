using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection
{
    public interface IIntrospectionClient
    {
        Task DownloadSchemaAsync(
            HttpClient client,
            Stream stream,
            CancellationToken cancellationToken = default);

        Task<DocumentNode> DownloadSchemaAsync(
            HttpClient client,
            CancellationToken cancellationToken = default);

        Task<ISchemaFeatures> GetSchemaFeaturesAsync(
            HttpClient client,
            CancellationToken cancellationToken = default);
    }
}
