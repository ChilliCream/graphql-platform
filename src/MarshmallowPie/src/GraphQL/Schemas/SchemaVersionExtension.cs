using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.Repositories;
using MarshmallowPie.Storage;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "SchemaVersion")]
    public class SchemaVersionExtension
    {
        public async Task<string> GetSourceTextAsync(
            [Parent]SchemaVersion schemaVersion,
            [Service]IFileStorage fileStorage,
            CancellationToken cancellationToken)
        {
            IFileContainer container = await fileStorage.GetContainerAsync(
                schemaVersion.Id.ToString("N", CultureInfo.InvariantCulture),
                cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<IFile> files = await container.GetFilesAsync().ConfigureAwait(false);

            IFile file = files.Single();

            using Stream stream = await file.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        public Task<Schema> GetSchemaAsync(
            [Parent]SchemaVersion schemaVersion,
            [DataLoader]SchemaByIdDataLoader dataLoader,
            CancellationToken cancellationToken) =>
            dataLoader.LoadAsync(schemaVersion.Id, cancellationToken);

        public IQueryable<SchemaPublishReport> GetPublishReports(
            [Parent]SchemaVersion schemaVersion,
            [Service]ISchemaRepository repository) =>
            repository.GetPublishReports().Where(t => t.SchemaVersionId == schemaVersion.Id);

        public async Task<SchemaPublishReport?> GetPublishReportByEnvironmentAsync(
            string environmentName,
            [Parent]SchemaVersion schemaVersion,
            [Service]ISchemaRepository schemaRepository,
            [Service]IEnvironmentRepository environmentRepository,
            CancellationToken cancellationToken)
        {
            Environment? environment = await environmentRepository.GetEnvironmentAsync(
                environmentName, cancellationToken)
                .ConfigureAwait(false);

            if (environment is null)
            {
                throw new GraphQLException(
                    $"The specified environment `{environmentName}` does not exist.");
            }

            return await schemaRepository.GetPublishReportAsync(
                schemaVersion.Id, environment.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
