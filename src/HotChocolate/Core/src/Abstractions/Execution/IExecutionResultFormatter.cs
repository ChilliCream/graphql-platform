using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

public interface IExecutionResultFormatter
{
    ValueTask FormatAsync(
        IExecutionResult result,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}
