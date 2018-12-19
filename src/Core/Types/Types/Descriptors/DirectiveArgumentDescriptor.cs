using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    internal class DirectiveArgumentDescriptor
        : ArgumentDescriptor
        , IDirectiveArgumentDescriptor
        , IDescriptionFactory<DirectiveArgumentDescription>
    {
        public DirectiveArgumentDescriptor(string argumentName)
            : base(new DirectiveArgumentDescription())
        {
            InputDescription.Name = argumentName;
            InputDescription.DefaultValue = NullValueNode.Default;
        }

        public DirectiveArgumentDescriptor(
            string argumentName, PropertyInfo property)
            : this(argumentName)
        {
            InputDescription.Description = property.GetGraphQLDescription();
            InputDescription.Property = property;
            InputDescription.TypeReference = property.GetInputType();
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
