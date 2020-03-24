using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public ResultTypeDescriptor(
            string name,
            bool isNullable,
            bool isList,
            bool isReferenceType,
            IReadOnlyList<ResultFieldDescriptor> fields)
        {
            Name = name;
            IsNullable = isNullable;
            IsList = isList;
            IsReferenceType = isReferenceType;
            Fields = fields;
        }

        public string Name { get; }

        public bool IsNullable { get; }

        public bool IsList { get; }

        public bool IsReferenceType { get; }

        public IReadOnlyList<ResultFieldDescriptor> Fields { get; }
    }
}
