using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IQueryResultSerializer
    {
        Task SerializeAsync(IQueryResultSerializer result, Stream stream);
    }
}
