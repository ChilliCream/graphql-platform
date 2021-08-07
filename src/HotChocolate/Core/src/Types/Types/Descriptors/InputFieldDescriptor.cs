using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// A fluent configuration API for GraphQL fields.
    /// </summary>
    public class InputFieldDescriptor
        : ArgumentDescriptorBase<InputFieldDefinition>
        , IInputFieldDescriptor
    {
        protected internal InputFieldDescriptor(
            IDescriptorContext context,
            NameString fieldName)
            : base(context)
        {
            Definition.Name = fieldName;
        }

        protected internal InputFieldDescriptor(
            IDescriptorContext context,
            InputFieldDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected internal InputFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.InputObjectField);
            Definition.Type = context.TypeInspector.GetInputReturnTypeRef(property);

            if (context.TypeInspector.TryGetDefaultValue(property, out object defaultValue))
            {
                Definition.RuntimeDefaultValue = defaultValue;
            }

            if (context.Naming.IsDeprecated(property, out var reason))
            {
                Deprecated(reason);
            }
        }

        protected override void OnCreateDefinition(InputFieldDefinition definition)
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
        public new IInputFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        /// <inheritdoc />
        public IInputFieldDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Deprecated(string? reason)
        {
            base.Deprecated(reason);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Deprecated()
        {
            base.Deprecated();
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public IInputFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor DefaultValue(
            object value)
        {
            base.DefaultValue(value);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        /// <inheritdoc />
        public new IInputFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static InputFieldDescriptor New(
            IDescriptorContext context,
            NameString fieldName) =>
            new(context, fieldName);

        public static InputFieldDescriptor New(
            IDescriptorContext context,
            PropertyInfo property) =>
            new(context, property);

        public static InputFieldDescriptor From(
            IDescriptorContext context,
            InputFieldDefinition definition) =>
            new(context, definition);
    }
}
