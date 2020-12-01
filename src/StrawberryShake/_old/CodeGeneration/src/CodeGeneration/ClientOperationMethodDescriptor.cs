using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ClientOperationMethodDescriptor
        : ICodeDescriptor
    {
        public ClientOperationMethodDescriptor(
            string name,
            string operationModelName,
            bool isStreamExecutor,
            string returnType,
            IReadOnlyList<ClientOperationMethodParameterDescriptor> parameters)
        {
            Name = name;
            OperationModelName = operationModelName;
            IsStreamExecutor = isStreamExecutor;
            ResponseModelName = returnType;
            Parameters = parameters;
        }

        public string Name { get; }

        public string OperationModelName { get; }

        public bool IsStreamExecutor { get; }

        public string ResponseModelName { get; }

        public IReadOnlyList<ClientOperationMethodParameterDescriptor> Parameters { get; }
    }
}
