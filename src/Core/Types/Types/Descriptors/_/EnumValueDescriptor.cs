using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class EnumValueDescriptor
        : DescriptorBase<EnumValueDefinition>
        , IEnumValueDescriptor
    {
        public EnumValueDescriptor(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Definition.Name = value.ToString().ToUpperInvariant();
            Definition.Value = value;
        }

        protected override EnumValueDefinition Definition { get; }

        public IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition)
        {
            Definition.SyntaxNode = enumValueDefinition;
            return this;
        }

        public IEnumValueDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IEnumValueDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IEnumValueDescriptor DeprecationReason(string value)
        {
            Definition.Description = value;
            return this;
        }

        public static EnumValueDescriptor New(object value) =>
            new EnumValueDescriptor(value);
    }
}
