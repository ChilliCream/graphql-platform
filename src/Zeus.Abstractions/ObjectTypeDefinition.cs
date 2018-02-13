using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeus.Abstractions
{
    public class ObjectTypeDefinition
        : IObjectTypeDefinition
    {
        private string _stringRepresentation;

        public ObjectTypeDefinition(string name,
            IEnumerable<FieldDefinition> fields)
            : this(name, fields, null)
        {
        }

        public ObjectTypeDefinition(string name,
            IEnumerable<FieldDefinition> fields,
            IEnumerable<string> interfaces)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A type definition name must not be null or empty.", nameof(name));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Name = name;
            Fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
            Interfaces = interfaces == null
                ? Array.Empty<string>()
                : interfaces.Distinct(StringComparer.Ordinal).ToArray();
        }

        private ObjectTypeDefinition(string name,
            IReadOnlyDictionary<string, FieldDefinition> fields,
            string[] interfaces)
        {
            Name = name;
            Fields = fields;
            Interfaces = interfaces;
        }

        public string Name { get; }
        
        public IReadOnlyCollection<string> Interfaces { get; }

        public IReadOnlyDictionary<string, FieldDefinition> Fields { get; }

        public ObjectTypeDefinition Merge(ObjectTypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other.Name.Equals(Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The name of the other object type "
                    + "definition has to match with this object type definition "
                    + "in order to merge them.", nameof(other));
            }

            // merge fields
            IReadOnlyDictionary<string, FieldDefinition> mergedFields
                = TypeDefinitionMergeHelpers.MergeFields(this, other);

            // merge interfaces
            HashSet<string> mergedInterfaces = new HashSet<string>(Interfaces, StringComparer.Ordinal);
            foreach (string interfaceName in other.Interfaces)
            {
                mergedInterfaces.Add(interfaceName);
            }

            return new ObjectTypeDefinition(Name, mergedFields, mergedInterfaces.ToArray());
        }

        ITypeDefinition ITypeDefinition.Merge(ITypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is ObjectTypeDefinition o)
            {
                return Merge(o);
            }

            throw new ArgumentException("The specified other type definition "
                + "must be of the same type as this type definition.",
                nameof(other));
        }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();
                if (Interfaces.Any())
                {
                    sb.AppendLine($"type {Name} implements {string.Join(", ", Interfaces)}");                    
                }
                else
                {
                    sb.AppendLine($"type {Name}");
                }

                sb.AppendLine("{");

                foreach (FieldDefinition field in Fields.Values)
                {
                    sb.AppendLine($"  {field}");
                }

                sb.Append("}");

                _stringRepresentation = sb.ToString();
            }

            return _stringRepresentation;
        }
    }
}