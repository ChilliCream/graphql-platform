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
            Definition.Description =
                context.Naming.GetEnumValueDescription(value);
            Definition.DeprecationReason =
                context.Naming.GetDeprecationReason(value);
        }

        protected override EnumValueDefinition Definition { get; } =
            new EnumValueDefinition();

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
            Definition.DeprecationReason = reason;
            return this;
        }

        public IEnumValueDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IEnumValueDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IEnumValueDescriptor Directive(
            NameString name, params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static EnumValueDescriptor New(
            IDescriptorContext context,
            object value) =>
            new EnumValueDescriptor(context, value);
    }
}
