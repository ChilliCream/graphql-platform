using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Tools;

public abstract class CommandHandler<T>
{
    public abstract Task<int> ExecuteAsync(T arguments, CancellationToken cancellationToken);
}
