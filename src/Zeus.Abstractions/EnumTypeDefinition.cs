using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeus.Abstractions
{
    public class EnumTypeDefinition
       : ITypeDefinition
    {
        private string _stringRepresentation;

        public EnumTypeDefinition(string name, IEnumerable<string> values)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A type definition name must not be null or empty.", nameof(name));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Name = name;
            Values = new ReadOnlySet<string>(values, StringComparer.Ordinal);
        }

        public string Name { get; }
        public IReadOnlySet<string> Values { get; }

        public EnumTypeDefinition Merge(EnumTypeDefinition other)
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

            return new EnumTypeDefinition(Name, Values.Concat(other.Values));
        }

        ITypeDefinition ITypeDefinition.Merge(ITypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is EnumTypeDefinition o)
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
                
                sb.AppendLine($"enum {Name}");
                sb.AppendLine("{");

                foreach (string value in Values)
                {
                    sb.AppendLine($"  {value}");
                }

                sb.Append("}");

                _stringRepresentation = sb.ToString();
            }
            
            return _stringRepresentation;
        }
    }
}