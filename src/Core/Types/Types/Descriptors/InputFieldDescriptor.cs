using System;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldDescriptor
        : ArgumentDescriptor
        , IInputFieldDescriptor
        , IDescriptionFactory<InputFieldDescription>
    {
        public InputFieldDescriptor(NameString name)
            : base(new InputFieldDescription())
        {
            InputDescription.Name = name;
        }

        public InputFieldDescriptor(PropertyInfo property)
            : base(new InputFieldDescription())
        {
            InputDescription.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            InputDescription.Name = property.GetGraphQLName();
            InputDescription.Description = property.GetGraphQLDescription();
            InputDescription.TypeReference = property.GetInputType();
            InputDescription.AcquireNonNullStatus(property);
        }

        protected new InputFieldDescription InputDescription
            => (InputFieldDescription)base.InputDescription;

        public new InputFieldDescription CreateDescription()
        {
            InputDescription.RewriteClrType(c => c.GetInputType());
            return InputDescription;
        }

        protected void Name(NameString name)
        {
            InputDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        #region IInputFieldDescriptor

        IInputFieldDescriptor IInputFieldDescriptor.SyntaxNode(
            InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type<TInputType>()
        {
            Type<TInputType>();
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Ignore()
        {
            InputDescription.Ignored = true;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(
            IValueNode defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(
            object defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Directive<T>(T directive)
        {
            InputDescription.Directives.AddDirective(directive);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Directive<T>()
        {
            InputDescription.Directives.AddDirective(new T());
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            InputDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            InputDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
