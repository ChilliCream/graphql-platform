using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution.Caching
{
    public interface IPreparedOperationCache
    {
        bool TryGetOperation(string operationId, out IPreparedOperation document);

        void TryAddOperation(string operationId, IPreparedOperation document);
    }
}

