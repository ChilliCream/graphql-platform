using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IResultParser<T>
        : IResultParser
        where T : class
    {
        new Task<IOperationResult<T>> ParseAsync(
            Stream stream,
            CancellationToken cancellationToken);
    }
}
