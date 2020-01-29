using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface ISessionCreator
    {
        ValueTask<string> CreateSessionAsync(CancellationToken cancellationToken = default);
    }
}
