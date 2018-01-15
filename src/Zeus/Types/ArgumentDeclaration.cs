using System;

namespace Zeus.Types
{
    public class ArgumentDeclaration
    {
        public ArgumentDeclaration(string name, TypeDeclaration type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }
        public TypeDeclaration Type { get; }
    }
}