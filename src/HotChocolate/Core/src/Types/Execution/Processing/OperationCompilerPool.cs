using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class OperationCompilerPool : DefaultObjectPool<OperationCompiler>
{
    public OperationCompilerPool(InputParser inputParser)
        : base(new Policy(inputParser))
    {
    }

    public OperationCompilerPool(InputParser inputParser, int maximumRetained)
        : base(new Policy(inputParser), maximumRetained)
    {
    }

    private sealed class Policy : IPooledObjectPolicy<OperationCompiler>
    {
        private readonly InputParser _inputParser;

        public Policy(InputParser inputParser)
        {
            _inputParser = inputParser;
        }

        public OperationCompiler Create() => new(_inputParser);

        public bool Return(OperationCompiler obj) => true;
    }
}
