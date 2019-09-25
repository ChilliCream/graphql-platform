using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IResultParser
    {
        Type ResultType { get; }

        Task<IOperationResult> ParseAsync(
            Stream stream,
            CancellationToken cancellationToken);
    }
}
