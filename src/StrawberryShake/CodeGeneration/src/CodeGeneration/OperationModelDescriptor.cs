using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class OperationModelDescriptor
        : ICodeDescriptor
    {
        public OperationModelDescriptor(
            string name,
            string @namespace,
            string graphQLName,
            string resultType,
            string documentType,
            string operationKind,
            IReadOnlyList<OperationArgumentDescriptor> arguments)
        {
            Name = name;
            Namespace = @namespace;
            GraphQLName = graphQLName;
            ResultType = resultType;
            DocumentType = documentType;
            OperationKind = operationKind;
            Arguments = arguments;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string GraphQLName { get; }

        public string ResultType { get; }

        public string DocumentType { get; }

        public string OperationKind { get; }

        public IReadOnlyList<OperationArgumentDescriptor> Arguments { get; }
    }
}
