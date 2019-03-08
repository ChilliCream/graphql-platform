using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumValue
    {
        internal EnumValue(EnumValueDefinition description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (description.Value == null)
            {
                throw new ArgumentException(
                    "The inner value of enum value cannot be null or empty.",
                    nameof(description));
            }

            SyntaxNode = description.SyntaxNode;
            Name = string.IsNullOrEmpty(description.Name)
                ? description.Value.ToString()
                : description.Name;
            Description = description.Description;
            DeprecationReason = description.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(description.DeprecationReason);
            Value = description.Value;
        }

        public EnumValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public string DeprecationReason { get; }

        public bool IsDeprecated { get; }

        public object Value { get; }
    }
}
