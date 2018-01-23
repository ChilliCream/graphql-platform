using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Definitions
{
    public class InputObjectTypeDefinition
    {
        public InputObjectTypeDefinition(string name, IEnumerable<InputFieldDefinition> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            Name = name;
            Fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        private InputObjectTypeDefinition(string name, Dictionary<string, InputFieldDefinition> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            Name = name;
            Fields = fields;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, InputFieldDefinition> Fields { get; }

        public InputObjectTypeDefinition Merge(InputObjectTypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (!other.Name.Equals(Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The type names must match.", nameof(other));
            }

            Dictionary<string, InputFieldDefinition> fields = Fields.ToDictionary(
                t => t.Key, t => t.Value, StringComparer.Ordinal);

            foreach (InputFieldDefinition field in other.Fields.Values)
            {
                fields[field.Name] = field;
            }

            return new InputObjectTypeDefinition(Name, fields);
        }
    }
}