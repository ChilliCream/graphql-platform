using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Utilities
{
    public interface IHttpResultSerializer
    {
        string GetContentType(
            IExecutionResult result);

        int GetStatusCode(
            IExecutionResult result);

        ValueTask SerializeAsync(
            IExecutionResult result,
            Stream stream,
            CancellationToken cancellationToken);
    }
}
