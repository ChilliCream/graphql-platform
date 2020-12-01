using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IResultParser
    {
        Type ResultType { get; }

        Task ParseAsync(
            Stream stream,
            IOperationResultBuilder resultBuilder,
            CancellationToken cancellationToken);

        void Parse(
            ReadOnlySpan<byte> result,
            IOperationResultBuilder resultBuilder);
    }
}
