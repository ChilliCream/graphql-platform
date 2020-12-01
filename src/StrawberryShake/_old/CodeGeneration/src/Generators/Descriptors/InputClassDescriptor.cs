using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class InputClassDescriptor
        : IInputClassDescriptor
    {
        public InputClassDescriptor(
            string name,
            string ns,
            InputObjectType type,
            IReadOnlyList<IInputFieldDescriptor> fields)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public string Name { get; }

        public string Namespace { get; }

        public InputObjectType Type { get; }

        public IReadOnlyList<IInputFieldDescriptor> Fields { get; }

        public IEnumerable<ICodeDescriptor> GetChildren() =>
            Fields.Where(t => t.InputObjectType is { })
                .Select(t => t.InputObjectType)
                .Cast<ICodeDescriptor>();
    }
}
