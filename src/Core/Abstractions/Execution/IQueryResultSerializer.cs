using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryResultSerializer
    {
        Task SerializeAsync(IReadOnlyQueryResult result, Stream stream);
    }
}
