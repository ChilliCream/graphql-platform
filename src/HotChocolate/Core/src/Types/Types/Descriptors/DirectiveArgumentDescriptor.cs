using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.MemberKind;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveArgumentDescriptor
        : ArgumentDescriptorBase<DirectiveArgumentDefinition>
        , IDirectiveArgumentDescriptor
    {
        protected internal DirectiveArgumentDescriptor(
            IDescriptorContext context,
            NameString argumentName)
            : base(context)
        {
            Definition.Name = argumentName;
        }

        protected internal DirectiveArgumentDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Name =
                context.Naming.GetMemberName(property, DirectiveArgument);
            Definition.Description =
                context.Naming.GetMemberDescription(property, DirectiveArgument);

            Definition.Type = context.TypeInspector.GetInputReturnTypeRef(property);
            Definition.Property = property;

            if (context.TypeInspector.TryGetDefaultValue(property, out object defaultValue))
            {
                Definition.RuntimeDefaultValue = defaultValue;
            }

            if (context.Naming.IsDeprecated(property, out var reason))
            {
                Deprecated(reason);
            }
        }

        protected internal DirectiveArgumentDescriptor(
            IDescriptorContext context,
            DirectiveArgumentDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected override void OnCreateDefinition(DirectiveArgumentDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.Property is not null)
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.Property);
                Definition.AttributesAreApplied = true;
            }

            base.OnCreateDefinition(definition);
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        /// <inheritdoc />
        public IDirectiveArgumentDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor DefaultValue(IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Deprecated(string value)
        {
            base.Deprecated(value);
            return this;
        }

        /// <inheritdoc />
        public new IDirectiveArgumentDescriptor Deprecated()
        {
            base.Deprecated();
            return this;
        }

        /// <inheritdoc />
        public IDirectiveArgumentDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public static DirectiveArgumentDescriptor New(
            IDescriptorContext context,
            NameString argumentName) =>
            new DirectiveArgumentDescriptor(context, argumentName);

        public static DirectiveArgumentDescriptor New(
            IDescriptorContext context,
            PropertyInfo property) =>
            new DirectiveArgumentDescriptor(context, property);

        public static DirectiveArgumentDescriptor From(
            IDescriptorContext context,
            DirectiveArgumentDefinition definition) =>
            new DirectiveArgumentDescriptor(context, definition);
    }
}
