using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationSerializer
    {
        Task SerializeAsync(IOperation operation, Stream requestStream);
    }
}
