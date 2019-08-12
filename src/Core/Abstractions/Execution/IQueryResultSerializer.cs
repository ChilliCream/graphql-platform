using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryResultSerializer
    {
        string ContentType { get; }

        Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream);

        Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream,
            CancellationToken cancellationToken);
    }
}
