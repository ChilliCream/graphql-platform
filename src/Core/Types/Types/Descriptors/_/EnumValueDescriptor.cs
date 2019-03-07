using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class EnumValueDescriptor
        : DescriptorBase<EnumValueDefinition>
        , IEnumValueDescriptor
    {
        public EnumValueDescriptor(IDescriptorContext context, object value)
            : base(context)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Definition.Name = context.Naming.GetEnumValueName(value);
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

        public IEnumValueDescriptor DeprecationReason(string reason)
        {
            Definition.Description = reason;
            return this;
        }

        public static EnumValueDescriptor New(
            IDescriptorContext context,
            object value) =>
            new EnumValueDescriptor(context, value);
    }
}
