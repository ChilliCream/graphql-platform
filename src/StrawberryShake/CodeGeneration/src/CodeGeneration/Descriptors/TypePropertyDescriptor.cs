using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class TypePropertyDescriptor
        : ICodeDescriptor
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the property
        /// </summary>
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
