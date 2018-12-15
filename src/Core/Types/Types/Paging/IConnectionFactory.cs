using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Paging
{
    public interface IConnectionFactory
    {
        Task<IConnection> CreateAsync(CancellationToken cancellationToken);
    }
}
