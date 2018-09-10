using System;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class DirectiveArgumentDescriptor
        : ArgumentDescriptor
        , IDirectiveArgumentDescriptor
        , IDescriptionFactory<DirectiveArgumentDescription>
    {
        public DirectiveArgumentDescriptor(string argumentName)
            : base(argumentName)
        {
        }

        public DirectiveArgumentDescriptor(
            string argumentName, Type argumentType)
            : base(argumentName, argumentType)
        {
        }

        public DirectiveArgumentDescriptor(
            string argumentName, PropertyInfo property)
            : base(argumentName, property.PropertyType)
        {
            InputDescription.Property = property;
        }

        protected new DirectiveArgumentDescription InputDescription
            => (DirectiveArgumentDescription)base.InputDescription;

        public new DirectiveArgumentDescription CreateDescription()
        {
            return InputDescription;
        }

        protected void Ignore()
        {
            InputDescription.Ignored = true;
        }

        #region IDirectiveArgumentDescriptor

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.DefaultValue(
            IValueNode defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.DefaultValue(
            object defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.Ignore()
        {
            Ignore();
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.SyntaxNode(
            InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.Type<TInputType>()
        {
            Type<TInputType>();
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.Type(
            ITypeNode type)
        {
            Type(type);
            return this;
        }

        #endregion
    }
}
