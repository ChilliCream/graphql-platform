using System;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class EnumValueDescriptor
        : IEnumValueDescriptor
    {
        public EnumValueDescriptor(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = value.ToString().ToUpperInvariant();
            Value = value;
        }

        public EnumValueDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public string DeprecationReason { get; protected set; }

        public object Value { get; protected set; }

        #region IEnumValueDescriptor

        IEnumValueDescriptor IEnumValueDescriptor.SyntaxNode(EnumValueDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.Name(string name)
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
                    "The specified name is not a valid GraphQL enum value name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IEnumValueDescriptor IEnumValueDescriptor.DeprecationReason(string deprecationReason)
        {
            DeprecationReason = deprecationReason;
            return this;
        }

        #endregion
    }
}
