using System;
using System.Collections.Immutable;

namespace HotChocolate.Types
{
    internal class InputObjectTypeDescriptor
        : IInputObjectTypeDescriptor
    {
        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public ImmutableList<InputFieldDescriptor> Fields { get; protected set; } =
            ImmutableList<InputFieldDescriptor>.Empty;

        #region IInputObjectTypeDescriptor

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Name(string name)
        {
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Description(string name)
        {
            Description = Description;
        }

        #endregion
    }
}
