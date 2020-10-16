using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate
{
    public interface IQuery<T> : IQuery
    {
        new ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken);
    }
}
