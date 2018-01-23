using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Definitions
{
    public class FieldDefinition
    {
        public FieldDefinition(string name, TypeDefinition type, IEnumerable<InputFieldDefinition> arguments)
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
        public TypeDefinition Type { get; }
        public IReadOnlyDictionary<string, InputFieldDefinition> Arguments { get; }
    }
}