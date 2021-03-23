using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Caching
{
    public interface IPreparedOperationCache
    {
        bool TryGetOperation(
            string operationId,
            [NotNullWhen(true)] out IPreparedOperation? operation);

        void TryAddOperation(
            string operationId,
            IPreparedOperation operation);

        void Clear();
    }
}

