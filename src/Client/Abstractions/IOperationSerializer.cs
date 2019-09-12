using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationSerializer
    {
        Task SerializeAsync(
            IOperation operation,
            IReadOnlyDictionary<string, object?>? extensions,
            bool includeDocument,
            Stream requestStream);
    }
}
