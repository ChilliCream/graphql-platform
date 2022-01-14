using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface ISchemaDocumentResolver
{
    Task<DocumentNode> GetDocumentAsync(CancellationToken cancellationToken = default);
}
