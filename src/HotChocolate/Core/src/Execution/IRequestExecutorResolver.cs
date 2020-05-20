using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IRequestExecutorResolver
    {
        ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            string? name = null, 
            CancellationToken cancellationToken = default);
    }
}