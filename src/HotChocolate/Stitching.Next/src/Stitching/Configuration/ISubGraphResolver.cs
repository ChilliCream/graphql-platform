using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface ISubGraphResolver
{
    Task<DocumentNode> GetDocumentAsync(
        CancellationToken cancellationToken = default);
}
