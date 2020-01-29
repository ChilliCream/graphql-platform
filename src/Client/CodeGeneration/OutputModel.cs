using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class OutputModelDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public IReadOnlyList<string> Implements { get; }

        public IReadOnlyList<OutputFieldDescriptor> Fields { get; }
    }
}
