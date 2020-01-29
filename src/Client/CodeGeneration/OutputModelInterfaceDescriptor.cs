using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class OutputModelInterfaceDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public IReadOnlyList<string> Implements { get; }

        public IReadOnlyList<OutputFieldDescriptor> Fields { get; }
    }
}
