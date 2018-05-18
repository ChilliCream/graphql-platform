using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class ObjectTypeBinding
    {
        public ObjectTypeBinding(string name, Type type,
            ObjectType objectType, IEnumerable<FieldBinding> fields)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Name = name;
            Type = type;
            ObjectType = objectType;
            Fields = fields.ToImmutableDictionary(t => t.Name);
        }

        public string Name { get; }
        public Type Type { get; }
        public ObjectType ObjectType { get; }
        public ImmutableDictionary<string, FieldBinding> Fields { get; }
    }
}
