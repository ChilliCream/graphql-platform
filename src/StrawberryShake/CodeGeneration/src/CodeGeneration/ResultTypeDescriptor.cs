using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public ResultTypeDescriptor(
            string name,
            IReadOnlyList<ResultTypeComponentDescriptor> components,
            IReadOnlyList<ResultFieldDescriptor> fields)
        {
            Name = name;
            Components = components;
            Fields = fields;
        }

        public string Name { get; }

        public IReadOnlyList<ResultTypeComponentDescriptor> Components { get; }

        public IReadOnlyList<ResultFieldDescriptor> Fields { get; }
    }
}
