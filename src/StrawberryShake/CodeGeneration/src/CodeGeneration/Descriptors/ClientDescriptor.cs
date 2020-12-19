using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ClientDescriptor : ICodeDescriptor
    {
        public string Name { get; }
        public List<OperationDescriptor> Operations { get; }

        public ClientDescriptor(string name, List<OperationDescriptor> operations)
        {
            Name = name;
            Operations = operations;
        }
    }
}
