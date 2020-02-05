using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.Storage;

namespace MarshmallowPie.GraphQL.Clients
{
    [ExtendObjectType(Name = "QueryDocument")]
    public class QueryDocumentExtension
    {
        public async Task<string> GetSourceTextAsync(
            [Parent]QueryDocument queryDocument,
            [Service]IFileStorage fileStorage,
            CancellationToken cancellationToken)
        {
            IFileContainer container = await fileStorage.GetContainerAsync(
                queryDocument.SchemaId.ToString("N", CultureInfo.InvariantCulture) + "_queries",
                cancellationToken)
                .ConfigureAwait(false);

            IFile file = await container.GetFileAsync(
                queryDocument.Id.ToString("N", CultureInfo.InvariantCulture),
                cancellationToken)
                .ConfigureAwait(false);

            using Stream stream = await file.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
