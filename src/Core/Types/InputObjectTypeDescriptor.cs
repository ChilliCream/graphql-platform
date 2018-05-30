using System;
using System.Collections.Immutable;
using HotChocolate.Internal;

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
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Description(string name)
        {
            Description = Description;
            return this;
        }

        #endregion
    }
}
