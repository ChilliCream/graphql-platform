using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class TypePropertyDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }
        public TypeReferenceDescriptor TypeReference { get; }


        public TypePropertyDescriptor(
            TypeReferenceDescriptor typeReference,
            string name)
        {
            TypeReference = typeReference;
            Name = name;
        }
    }
}
