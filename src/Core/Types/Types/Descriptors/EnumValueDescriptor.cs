using System;
using HotChocolate.Utilities;
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

        protected void Name(NameString name)
        {
            ValueDescription.Name = name.EnsureNotEmpty(nameof(name));
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

        IEnumValueDescriptor IEnumValueDescriptor.Name(NameString name)
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
