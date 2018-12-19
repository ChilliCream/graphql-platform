using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class ObjectTypeBinding
        : ITypeBinding
    {
        public ObjectTypeBinding(
            NameString name,
            Type type,
            ObjectType objectType,
            IEnumerable<FieldBinding> fields)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Name = name;
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            ObjectType = objectType
                ?? throw new ArgumentNullException(nameof(objectType));
            Fields = fields.ToImmutableDictionary(t => t.Name);
        }

        public NameString Name { get; }
        public Type Type { get; }
        public ObjectType ObjectType { get; }
        public IReadOnlyDictionary<NameString, FieldBinding> Fields { get; }
    }
}
