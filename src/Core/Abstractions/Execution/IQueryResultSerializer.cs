using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryResultSerializer
    {
        string ContentType { get; }

        ValueTask SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream,
            CancellationToken cancellationToken = default);
    }
}
