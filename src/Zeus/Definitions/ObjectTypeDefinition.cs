using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Definitions
{
    public class ObjectTypeDefinition
    {
        public ObjectTypeDefinition(string name, IEnumerable<FieldDefinition> fields)
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

        private ObjectTypeDefinition(string name, Dictionary<string, FieldDefinition> fields)
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
        public IReadOnlyDictionary<string, FieldDefinition> Fields { get; }

        public ObjectTypeDefinition Merge(ObjectTypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (!other.Name.Equals(Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The type names must match.", nameof(other));
            }

            Dictionary<string, FieldDefinition> fields = Fields.ToDictionary(
                t => t.Key, t => t.Value, StringComparer.Ordinal);

            foreach (FieldDefinition field in other.Fields.Values)
            {
                fields[field.Name] = field;
            }

            return new ObjectTypeDefinition(Name, fields);
        }
    }
}