using System.Threading.Tasks;
using System.Threading;

namespace StrawberryShake.Tools
{
    public abstract class CommandHandler<T>
    {
        public abstract Task<int> ExecuteAsync(T arguments, CancellationToken cancellationToken);
    }
}
