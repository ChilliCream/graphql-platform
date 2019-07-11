using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public interface IPayloadSerializer
    {
        Task<byte[]> SerializeAsync(object value);

        Task<object> DeserializeAsync(byte[] content);
    }
}
