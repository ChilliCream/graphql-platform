using System;
using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public class UnionTypeDefinition
        : ITypeDefinition
    {
        private string _stringRepresentation;

        public UnionTypeDefinition(string name, params NamedType[] types)
            : this(name, (IEnumerable<NamedType>)types)
        {
        }

        public UnionTypeDefinition(string name, IEnumerable<NamedType> types)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A type definition name must not be null or empty.", nameof(name));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Name = name;
            Types = new ReadOnlySet<NamedType>(types);

            if (Types.Count < 2)
            {
                throw new ArgumentException("A union type must at least consist of two types.");
            }
        }

        private UnionTypeDefinition(string name, HashSet<NamedType> types)
        {
            Name = name;
            Types = new ReadOnlySet<NamedType>(types);
        }

        public string Name { get; }

        public IReadOnlySet<NamedType> Types { get; }

        public UnionTypeDefinition Merge(UnionTypeDefinition other)
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

            HashSet<NamedType> mergedTypes = new HashSet<NamedType>(Types);
            foreach (NamedType type in other.Types)
            {
                mergedTypes.Add(type);
            }

            return new UnionTypeDefinition(Name, mergedTypes);
        }

        ITypeDefinition ITypeDefinition.Merge(ITypeDefinition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other is UnionTypeDefinition u)
            {
                return Merge(u);
            }

            throw new ArgumentException("The specified other type definition "
                + "must be of the same type as this type definition.",
                nameof(other));
        }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                _stringRepresentation = $"union {Name} = {string.Join(" | ", Types)}";
            }

            return _stringRepresentation;
        }
    }
}