using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveArgumentDescriptor
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
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IDirectiveArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type<TInputType>(inputType);
            return this;
        }

        public new IDirectiveArgumentDescriptor Type(
            ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IDirectiveArgumentDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IDirectiveArgumentDescriptor DefaultValue(
            object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public IDirectiveArgumentDescriptor Ignore()
        {
            InputDescription.Ignore = true;
            return this;
        }
    }
}
