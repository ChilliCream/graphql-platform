using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class EnumValueDescriptor
        : IEnumValueDescriptor
        , IDefinitionFactory<EnumValueDescription>
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

        DescriptionBase IDescriptionFactory.CreateDescription() =>
            CreateDescription();

        public IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition)
        {
            ValueDescription.SyntaxNode = enumValueDefinition;
            return this;
        }

        public IEnumValueDescriptor Name(NameString value)
        {
            ValueDescription.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IEnumValueDescriptor Description(string value)
        {
            ValueDescription.Description = value;
            return this;
        }

        public IEnumValueDescriptor DeprecationReason(string value)
        {
            ValueDescription.Description = value;
            return this;
        }
    }
}
