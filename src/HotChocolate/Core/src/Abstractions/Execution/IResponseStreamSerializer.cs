using System.Threading;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IResponseStreamSerializer
    {
        Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken = default);
    }
}
