using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public class InputFieldDescriptor
        : IInputFieldDescriptor
    {
        public InputFieldDescriptor(
            string name,
            IType type,
            IInputField field,
            IInputClassDescriptor? inputObjectType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            InputObjectType = inputObjectType;
        }

        public string Name { get; }

        public IType Type { get; }

        public IInputField Field { get; }

        public IInputClassDescriptor? InputObjectType { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
