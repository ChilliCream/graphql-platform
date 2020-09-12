using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Utilities
{
    public interface IHttpResultSerializer
    {
        string GetContentType(IExecutionResult result);

        HttpStatusCode GetStatusCode(IExecutionResult result);

        ValueTask SerializeAsync(
            IExecutionResult result,
            Stream stream,
            CancellationToken cancellationToken);
    }
}
