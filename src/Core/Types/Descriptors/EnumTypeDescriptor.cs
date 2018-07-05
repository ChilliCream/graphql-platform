using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumTypeDescriptor
        : IEnumTypeDescriptor
        , IDescriptionFactory<EnumTypeDescription>
    {
        private bool _valuesInitialized;

        public EnumTypeDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            EnumDescription.Name = name;
        }

        public EnumTypeDescriptor(Type enumType)
        {
            EnumDescription.NativeType = enumType
                ?? throw new ArgumentNullException(nameof(enumType));
            EnumDescription.Name = enumType.GetGraphQLName();
        }

        protected List<EnumValueDescriptor> Values { get; } =
            new List<EnumValueDescriptor>();

        protected EnumTypeDescription EnumDescription { get; } =
            new EnumTypeDescription();

        public EnumTypeDescription CreateDescription()
        {
            return EnumDescription;
        }

        protected virtual void CompleteValues()
        {
            AddImplicitValues();
        }

        protected void AddImplicitValues()
        {
            if (EnumDescription.ValueBindingBehavior == BindingBehavior.Implicit)
            {
                if (!_valuesInitialized
                    && EnumDescription.NativeType != null
                    && EnumDescription.NativeType.IsEnum)
                {
                    foreach (object o in Enum.GetValues(
                        EnumDescription.NativeType))
                    {
                        Values.Add(new EnumValueDescriptor(o));
                    }
                    _valuesInitialized = true;
                }
            }
        }

        protected void SyntaxNode(EnumTypeDefinitionNode syntaxNode)
        {
            EnumDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
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

            EnumDescription.Name = name;
        }

        protected void Description(string description)
        {
            EnumDescription.Description = description;
        }

        protected EnumValueDescriptor Item<T>(T value)
        {
            if (EnumDescription.NativeType == null)
            {
                EnumDescription.NativeType = typeof(T);
            }

            var descriptor = new EnumValueDescriptor(value);
            Values.Add(descriptor);
            return descriptor;
        }

        protected void BindItems(BindingBehavior bindingBehavior)
        {
            EnumDescription.ValueBindingBehavior = bindingBehavior;
        }

        #region IEnumTypeDescriptor

        IEnumTypeDescriptor IEnumTypeDescriptor.SyntaxNode(
            EnumTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IEnumValueDescriptor IEnumTypeDescriptor.Item<T>(T value)
        {
            return Item(value);
        }

        IEnumTypeDescriptor IEnumTypeDescriptor.BindItems(
            BindingBehavior bindingBehavior)
        {
            BindItems(bindingBehavior);
            return this;
        }

        #endregion
    }

    internal class EnumTypeDescriptor<T>
        : EnumTypeDescriptor
        , IEnumTypeDescriptor<T>
    {
        public EnumTypeDescriptor()
            : base(typeof(T))
        {
        }

        #region IEnumTypeDescriptor<T>

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.SyntaxNode(
            EnumTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Name(string name)
        {
            Name(name);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IEnumTypeDescriptor<T> IEnumTypeDescriptor<T>.BindItems(
            BindingBehavior bindingBehavior)
        {
            BindItems(bindingBehavior);
            return this;
        }

        IEnumValueDescriptor IEnumTypeDescriptor<T>.Item(T value)
        {
            return Item(value);
        }

        #endregion
    }
}
