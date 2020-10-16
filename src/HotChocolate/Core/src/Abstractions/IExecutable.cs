using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate
{
    public interface IQuery
    {
        ValueTask<object> ExecuteAsync(CancellationToken cancellationToken);

        string Print();
    }
}
