using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Types
{
    public class FieldDeclaration
    {
        public FieldDeclaration(string name, TypeDeclaration type, IEnumerable<InputFieldDeclaration> arguments)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (arguments == null)
            {
                throw new System.ArgumentNullException(nameof(arguments));
            }

            Name = name;
            Type = type;
            Arguments = arguments.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }
        public TypeDeclaration Type { get; }
        public IReadOnlyDictionary<string, InputFieldDeclaration> Arguments { get; }
    }
}