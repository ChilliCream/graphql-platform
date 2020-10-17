using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate
{
    public interface IExecutable<T> : IExecutable
    {
        new ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken);
    }
}
