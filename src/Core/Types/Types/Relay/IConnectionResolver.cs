using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Relay
{
    public interface IConnectionResolver
    {
        Task<IConnection> ResolveAsync(CancellationToken cancellationToken);
    }
}
