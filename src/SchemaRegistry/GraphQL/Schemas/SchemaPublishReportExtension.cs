using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Environments;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "SchemaPublishReport")]
    public class SchemaPublishReportExtension
    {
        public Task<Environment> GetEnvironmentAsync(
            [Parent]SchemaPublishReport report,
            [DataLoader]EnvironmentByIdDataLoader dataLoader,
            CancellationToken cancellationToken) =>
            dataLoader.LoadAsync(report.EnvironmentId, cancellationToken);

        public Task<SchemaVersion> GetSchemaVersionAsync(
            [Parent]SchemaPublishReport report,
            [DataLoader]SchemaVersionByIdDataLoader dataLoader,
            CancellationToken cancellationToken) =>
            dataLoader.LoadAsync(report.SchemaVersionId, cancellationToken);
    }
}
