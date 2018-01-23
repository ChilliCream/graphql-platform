using System;

namespace Zeus.Definitions
{
    public class InputFieldDefinition
    {
        public InputFieldDefinition(string name, TypeDefinition type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }
        public TypeDefinition Type { get; }
    }
}