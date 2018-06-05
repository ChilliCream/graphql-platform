using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;

namespace HotChocolate.Types
{
    internal class EnumTypeDescriptor
        : IEnumTypeDescriptor
    {
        public EnumTypeDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }
            Name = name;
        }

        public EnumTypeDescriptor(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }

            Name = enumType.GetGraphQLName();
        }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public ImmutableList<EnumValueDescriptor> Items { get; protected set; } =
            ImmutableList<EnumValueDescriptor>.Empty;

        public IEnumerable<EnumValue> CreateEnumValues()
        {
            if (NativeType != null && NativeType.IsEnum && !Items.Any())
            {
                foreach (object o in Enum.GetValues(NativeType))
                {
                    Items = Items.Add(new EnumValueDescriptor(o));
                }
            }

            return Items.Select(t => new EnumValue(new EnumValueConfig
            {
                Name = t.Name,
                Description = t.Description,
                DeprecationReason = t.DeprecationReason,
                Value = t.Value
            }));
        }

        #region IEnumTypeDescriptor

        IEnumTypeDescriptor IEnumTypeDescriptor.Name(string name)
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

        IEnumTypeDescriptor IEnumTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IEnumValueDescriptor IEnumTypeDescriptor.Item<T>(T value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(
                    "An enum value mustn't be null.");
            }

            if (NativeType == null)
            {
                NativeType = typeof(T);
            }

            if (NativeType != typeof(T))
            {
                throw new ArgumentException(
                    "The item type has to be " +
                    $"{NativeType.FullName}.");
            }

            EnumValueDescriptor descriptor = new EnumValueDescriptor(value);
            Items = Items.Add(descriptor);
            return descriptor;
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
            NativeType = typeof(T);
        }

        #region IEnumTypeDescriptor<T>

        IEnumValueDescriptor IEnumTypeDescriptor<T>.Item(T value)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(
                    "An enum value mustn't be null.");
            }

            EnumValueDescriptor descriptor = new EnumValueDescriptor(value);
            Items = Items.Add(descriptor);
            return descriptor;
        }

        #endregion
    }
}
