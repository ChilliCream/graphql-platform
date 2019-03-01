using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface IRemoteQueryRequest
        : IReadOnlyQueryRequest
    {
        new DocumentNode Query { get; }
    }
}
