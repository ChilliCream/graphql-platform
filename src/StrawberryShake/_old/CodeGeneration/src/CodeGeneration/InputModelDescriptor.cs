using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class InputModelDescriptor
        : ICodeDescriptor
    {
        public InputModelDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<InputFieldDescriptor> fields)
        {
            Name = name;
            Namespace = @namespace;
            Fields = fields;
        }

        public string Name { get; }

        public string Namespace { get; }

        public IReadOnlyList<InputFieldDescriptor> Fields { get; }
    }
}
