using System;
using System.Collections.Immutable;
using HotChocolate.Internal;

namespace HotChocolate.Types
{
    internal class EnumTypeDescriptor
        : IEnumTypeDescriptor
    {
        public EnumTypeDescriptor(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }

            // TODO : move name resolution to utilities
            Name = enumType.GetGraphQLName();
            if (Name == enumType.Name && Name.EndsWith("Type"))
            {
                Name = Name.Substring(0, Name.Length - 4);
            }
        }

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public Type Type { get; protected set; }
        public ImmutableDictionary<string, object> Items { get; protected set; }
            = ImmutableDictionary<string, object>.Empty;

        #region IEnumTypeDescriptor

        IEnumTypeDescriptor IEnumTypeDescriptor.Name(string name)
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

        IEnumTypeDescriptor IEnumTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Item(string name)
        {
            // TODO : name validation
            string upperCaseName = Name.ToUpperInvariant();
            Items = Items.Add(upperCaseName, upperCaseName);
            return this;
        }

        #endregion
    }

    internal class EnumTypeDescriptor<T>
        : EnumTypeDescriptor
        , IEnumTypeDescriptor<T>
    {
        public EnumTypeDescriptor(Type enumType)
            : base(enumType)
        {
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Item(T value)
        {
            // TODO : handle null values
            string upperCaseName = value.ToString().ToUpperInvariant();
            Items = Items.Add(upperCaseName, value);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Item(string name, T value)
        {
            // TODO : name validation

            string upperCaseName = name.ToUpperInvariant();
            Items = Items.Add(upperCaseName, value);
            return this;
        }
    }
}
