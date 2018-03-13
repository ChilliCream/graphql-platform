using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.Abstractions
{
    public class InterfaceTypeDefinition
       : IObjectTypeDefinition
    {
        private string _stringRepresentation;

        public InterfaceTypeDefinition(string name,
            IEnumerable<FieldDefinition> fields)
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
        }

        private InterfaceTypeDefinition(string name,
            IReadOnlyDictionary<string, FieldDefinition> fields)
        {
            Name = name;
            Fields = fields;
        }

        public string Name { get; }
        
        public IReadOnlyDictionary<string, FieldDefinition> Fields { get; }

        public InterfaceTypeDefinition Merge(InterfaceTypeDefinition other)
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

            return new InterfaceTypeDefinition(Name, mergedFields);
        }

        ITypeDefinition ITypeDefinition.Merge(ITypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is InterfaceTypeDefinition i)
            {
                return Merge(i);
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
                sb.AppendLine($"interface {Name}");
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