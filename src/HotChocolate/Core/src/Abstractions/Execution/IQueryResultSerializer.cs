using System.Threading;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IQueryResultSerializer
    {
        Task SerializeAsync(
            IQueryResult result,
            Stream stream,
            CancellationToken cancellationToken = default);
    }
}
