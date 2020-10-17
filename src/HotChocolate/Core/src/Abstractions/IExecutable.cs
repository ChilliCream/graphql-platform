using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate
{
    public interface IExecutable
    {
        ValueTask<object> ExecuteAsync(CancellationToken cancellationToken);

        string Print();
    }
}
