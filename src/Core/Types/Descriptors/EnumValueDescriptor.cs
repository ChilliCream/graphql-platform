using System;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumValueDescriptor
        : IEnumValueDescriptor
        , IDescriptionFactory<EnumValueDescription>
    {
        public EnumValueDescriptor(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ValueDescription.Name = value.ToString().ToUpperInvariant();
            ValueDescription.Value = value;
        }

        protected EnumValueDescription ValueDescription { get; } =
            new EnumValueDescription();

        public EnumValueDescription CreateDescription()
        {
            return ValueDescription;
        }

        protected void SyntaxNode(EnumValueDefinitionNode syntaxNode)
        {
            ValueDescription.SyntaxNode = syntaxNode;
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
                    "The specified name is not a valid " +
                    "GraphQL enum value name.",
                    nameof(name));
            }

            ValueDescription.Name = name;
        }

        protected void Description(string description)
        {
            ValueDescription.Description = description;
        }

        protected void DeprecationReason(string deprecationReason)
        {
            ValueDescription.DeprecationReason = deprecationReason;
        }


        #region IEnumValueDescriptor

        IEnumValueDescriptor IEnumValueDescriptor.SyntaxNode(
            EnumValueDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.DeprecationReason(
            string deprecationReason)
        {
            DeprecationReason(deprecationReason);
            return this;
        }

        #endregion
    }
}
