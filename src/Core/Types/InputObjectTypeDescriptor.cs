using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
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

            if (!ValidationHelper.IsTypeNameValid(name))
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

    internal class InputObjectTypeDescriptor<T>
        : InputObjectTypeDescriptor
        , IInputObjectTypeDescriptor<T>
    {
        IInputFieldDescriptor IInputObjectTypeDescriptor<T>.Field<TValue>(
            Expression<Func<T, TValue>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return new InputFieldDescriptor(p);
            }
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }
    }
}
