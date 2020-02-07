using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class OutputModelDescriptor
        : ICodeDescriptor
    {
        public OutputModelDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<string>? implements,
            IReadOnlyList<OutputFieldDescriptor> fields)
        {
            Name = name;
            Namespace = @namespace;
            Implements = implements ?? Array.Empty<string>();
            Fields = fields;
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<string> Implements { get; }

        public IReadOnlyList<OutputFieldDescriptor> Fields { get; }
    }

    public class ClientClassDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string InterfaceName { get; }

        public string OperationExecutorPool { get; }

        public string OperationExecutor { get; }

        public string OperationStreamExecutor { get; }

        public IReadOnlyList<ClientOperationMethodDescriptor> Operations { get; }
    }

    public class ClientOperationMethodDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string OperationModelName { get; }

        public bool IsStreamExecutor { get; }

        public string ReturnType { get; }

        public IReadOnlyList<ClientOperationMethodParameterDescriptor> Parameters { get; }
    }

    public class ClientOperationMethodParameterDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public string PropertyName { get; }

        public string TypeName { get; }

        public bool IsOptional { get; }

        public string Default { get; }
    }
}
