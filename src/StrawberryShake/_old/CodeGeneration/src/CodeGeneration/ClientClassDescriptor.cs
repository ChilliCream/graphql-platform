using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ClientClassDescriptor
        : ICodeDescriptor
    {
        public ClientClassDescriptor(
            string name,
            string interfaceName,
            string operationExecutorPool,
            string? operationExecutor,
            string? operationStreamExecutor,
            IReadOnlyList<ClientOperationMethodDescriptor> operations)
        {
            Name = name;
            InterfaceName = interfaceName;
            OperationExecutorPool = operationExecutorPool;
            OperationExecutor = operationExecutor;
            OperationStreamExecutor = operationStreamExecutor;
            Operations = operations;
        }

        public string Name { get; }

        public string InterfaceName { get; }

        public string OperationExecutorPool { get; }

        public string? OperationExecutor { get; }

        public string? OperationStreamExecutor { get; }

        public IReadOnlyList<ClientOperationMethodDescriptor> Operations { get; }
    }
}
