using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal class DirectiveArgumentDescriptor
        : ArgumentDescriptor
        , IDirectiveArgumentDescriptor
        , IDescriptionFactory<DirectiveArgumentDescription>
    {
        public DirectiveArgumentDescriptor(string argumentName)
            : base(new DirectiveArgumentDescription())
        {
            InputDescription =
                (DirectiveArgumentDescription)base.InputDescription;
            InputDescription.Name = argumentName;
            InputDescription.DefaultValue = NullValueNode.Default;
        }

        public DirectiveArgumentDescriptor(
            string argumentName, PropertyInfo property)
            : this(argumentName)
        {
            InputDescription.Description = property.GetGraphQLDescription();
            InputDescription.Property = property;
            InputDescription.Type = property.GetInputType();
        }

        protected new DirectiveArgumentDescription InputDescription { get; }

        public new DirectiveArgumentDescription CreateDescription()
        {
            return InputDescription;
        }

        public new IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            InputDescription.SyntaxNode = inputValueDefinition;
            return this;
        }

        public IDirectiveArgumentDescriptor Name(
            NameString value)
        {
            InputDescription.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }
        public new IDirectiveArgumentDescriptor Description(string value)
        {
            InputDescription.Description = value;
            return this;
        }

        public new IDirectiveArgumentDescriptor Type<TInputType>()
        {
            throw new System.NotImplementedException();
        }

        public new IDirectiveArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            throw new System.NotImplementedException();
        }

    public new IDirectiveArgumentDescriptor Type(ITypeNode type)
    {
        throw new System.NotImplementedException();
    }


    IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.DefaultValue(IValueNode defaultValue)
    {
        throw new System.NotImplementedException();
    }

    IDirectiveArgumentDescriptor IDirectiveArgumentDescriptor.DefaultValue(object defaultValue)
    {
        throw new System.NotImplementedException();
    }







    public IDirectiveArgumentDescriptor Ignore()
    {
        InputDescription.Ignore = true;
        return this;
    }
}
}
