using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class InputClassDescriptor
        : IInputClassDescriptor
    {
        public InputClassDescriptor(
            string name,
            InputObjectType type,
            IReadOnlyList<IInputFieldDescriptor> fields)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public string Name { get; }

        public InputObjectType Type { get; }

        public IReadOnlyList<IInputFieldDescriptor> Fields { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
