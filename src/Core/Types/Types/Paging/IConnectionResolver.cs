using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public interface IConnectionResolver
    {
        Task<IConnection> ResolveAsync(CancellationToken cancellationToken);
    }
}
