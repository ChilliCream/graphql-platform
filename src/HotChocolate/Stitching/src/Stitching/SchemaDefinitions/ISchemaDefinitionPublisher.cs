using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Stitching.SchemaDefinitions
{
    public interface ISchemaDefinitionPublisher
    {
        ValueTask PublishAsync(
            RemoteSchemaDefinition schemaDefinition,
            CancellationToken cancellationToken = default);
    }
}
