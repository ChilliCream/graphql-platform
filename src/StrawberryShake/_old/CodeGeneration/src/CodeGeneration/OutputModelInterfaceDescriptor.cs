using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class OutputModelInterfaceDescriptor
        : ICodeDescriptor
    {
        public OutputModelInterfaceDescriptor(
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
}
