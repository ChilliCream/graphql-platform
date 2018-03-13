using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.Abstractions
{
    public class InputObjectTypeDefinition
        : ITypeDefinition
    {
        private string _stringRepresentation;

        public InputObjectTypeDefinition(string name,
            IEnumerable<InputValueDefinition> fields)
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

        private InputObjectTypeDefinition(string name,
            IReadOnlyDictionary<string, InputValueDefinition> fields)
        {
            Name = name;
            Fields = fields;
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, InputValueDefinition> Fields { get; }

        public InputObjectTypeDefinition Merge(InputObjectTypeDefinition other)
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
            IReadOnlyDictionary<string, InputValueDefinition> mergedFields
                = TypeDefinitionMergeHelpers.MergeFields(this, other);

            return new InputObjectTypeDefinition(Name, mergedFields);
        }

        ITypeDefinition ITypeDefinition.Merge(ITypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is InputObjectTypeDefinition i)
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
                
                sb.AppendLine($"input {Name}");
                sb.AppendLine("{");

                foreach (InputValueDefinition field in Fields.Values)
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