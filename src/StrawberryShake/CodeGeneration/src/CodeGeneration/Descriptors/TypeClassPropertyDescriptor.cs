using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class TypeClassPropertyDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }
        public TypeDescriptor Type { get; }


        public TypeClassPropertyDescriptor(
            TypeDescriptor @type,
            string name)
        {
            Type = type;
            Name = name;
        }
    }
}
