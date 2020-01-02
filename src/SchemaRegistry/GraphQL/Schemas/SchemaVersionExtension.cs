using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "SchemaVersion")]
    public class SchemaVersionExtension
    {
        public Task<Schema> GetSchemaAsync(
            [Parent]SchemaVersion schemaVersion,
            [DataLoader]SchemaByIdDataLoader dataLoader,
            CancellationToken cancellationToken) =>
            dataLoader.LoadAsync(schemaVersion.Id, cancellationToken);
    }
}
