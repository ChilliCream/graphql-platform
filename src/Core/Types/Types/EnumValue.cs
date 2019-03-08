using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumValue
    {
        public EnumValue(EnumValueDefinition enumValueDefinition)
        {
            if (enumValueDefinition == null)
            {
                throw new ArgumentNullException(nameof(enumValueDefinition));
            }

            if (enumValueDefinition.Value == null)
            {
                // TODO : resources
                throw new ArgumentException(
                    "The inner value of enum value cannot be null or empty.",
                    nameof(enumValueDefinition));
            }

            SyntaxNode = enumValueDefinition.SyntaxNode;
            Name = enumValueDefinition.Name.HasValue
                ? enumValueDefinition.Name
                : (NameString)enumValueDefinition.Value.ToString();
            Description = enumValueDefinition.Description;
            DeprecationReason = enumValueDefinition.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(
                enumValueDefinition.DeprecationReason);
            Value = enumValueDefinition.Value;
        }

        public EnumValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public string DeprecationReason { get; }

        public bool IsDeprecated { get; }

        public object Value { get; }
    }
}
