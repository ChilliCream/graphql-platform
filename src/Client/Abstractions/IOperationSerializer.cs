using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationSerializer
    {
        Task SerializeAsync(
            IOperation operation,
            Stream stream,
            IReadOnlyDictionary<string, object?>? extensions = null,
            bool? includeDocument = null,
            CancellationToken cancellationToken = default);

        Task SerializeAsync(
            IOperation operation,
            IBufferWriter<byte> writer,
            IReadOnlyDictionary<string, object?>? extensions = null,
            bool? includeDocument = null,
            CancellationToken cancellationToken = default);
    }
}
