using System.Buffers;
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
            OperationSerializerOptions? options = null,
            CancellationToken cancellationToken = default);

        void Serialize(
            IOperation operation,
            IBufferWriter<byte> writer,
            OperationSerializerOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
