using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface IResponseStreamSerializer
    {
        Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream);

        Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken);
    }
}
