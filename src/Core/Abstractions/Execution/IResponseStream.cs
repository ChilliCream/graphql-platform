using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IResponseStream : IAsyncEnumerable<IReadOnlyQueryResult>
    {
    }
}
