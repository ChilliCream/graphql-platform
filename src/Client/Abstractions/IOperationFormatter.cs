using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationFormatter
    {
        Task SerializeAsync(
            IOperation operation,
            Stream stream,
            OperationFormatterOptions? options = null,
            CancellationToken cancellationToken = default);

        void Serialize(
            IOperation operation,
            IBufferWriter<byte> writer,
            OperationFormatterOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
