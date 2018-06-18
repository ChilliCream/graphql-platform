using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

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

        public EnumTypeDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public ImmutableList<EnumValueDescriptor> Items { get; protected set; } =
            ImmutableList<EnumValueDescriptor>.Empty;

        public BindingBehavior BindingBehavior { get; protected set; }

        public virtual IEnumerable<EnumValue> CreateEnumValues()
        {
            if (BindingBehavior == BindingBehavior.Implicit)
            {
                if (NativeType != null && NativeType.IsEnum && !Items.Any())
                {
                    foreach (object o in Enum.GetValues(NativeType))
                    {
                        Items = Items.Add(new EnumValueDescriptor(o));
                    }
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

        IEnumTypeDescriptor IEnumTypeDescriptor.SyntaxNode(
            EnumTypeDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

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

        IEnumTypeDescriptor IEnumTypeDescriptor.BindItems(BindingBehavior bindingBehavior)
        {
            BindingBehavior = bindingBehavior;
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
            NativeType = typeof(T);
        }

        #region IEnumTypeDescriptor<T>

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.SyntaxNode(EnumTypeDefinitionNode syntaxNode)
        {
            ((IEnumTypeDescriptor)this).SyntaxNode(syntaxNode);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Name(string name)
        {
            ((IEnumTypeDescriptor)this).Name(name);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Description(string description)
        {
            ((IEnumTypeDescriptor)this).Description(description);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.BindItems(BindingBehavior bindingBehavior)
        {
            BindingBehavior = bindingBehavior;
            return this;
        }

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
