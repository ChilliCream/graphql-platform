using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class InputObjectTypeBinding
        : ITypeBinding
    {
        public InputObjectTypeBinding(
            NameString name,
            Type type,
            InputObjectType inputObjectType,
            IEnumerable<InputFieldBinding> fields)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (inputObjectType == null)
            {
                throw new ArgumentNullException(nameof(inputObjectType));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Name = name.EnsureNotEmpty(nameof(name));
            Type = type;
            InputObjectType = inputObjectType;
            Fields = fields.ToImmutableDictionary(t => t.Name);
        }

        public NameString Name { get; }

        public Type Type { get; }

        public InputObjectType InputObjectType { get; }

        public IDictionary<NameString, InputFieldBinding> Fields { get; }
    }
}
