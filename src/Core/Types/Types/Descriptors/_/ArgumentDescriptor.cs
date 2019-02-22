using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal class ArgumentDescriptor
        : IArgumentDescriptor
        , IDescriptionFactory<ArgumentDescription>
    {
        protected ArgumentDescriptor(ArgumentDescription argumentDescription)
        {
            InputDescription = argumentDescription
                ?? throw new ArgumentNullException(nameof(argumentDescription));
        }

        public ArgumentDescriptor(string argumentName, Type argumentType)
            : this(argumentName)
        {
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            InputDescription = new ArgumentDescription();
            InputDescription.Name = argumentName;
            InputDescription.Type = argumentType.GetInputType();
            InputDescription.DefaultValue = NullValueNode.Default;
        }

        public ArgumentDescriptor(NameString argumentName)
        {
            InputDescription = new ArgumentDescription
            {
                Name = argumentName.EnsureNotEmpty(nameof(argumentName)),
                DefaultValue = NullValueNode.Default
            };
        }

        protected ArgumentDescription InputDescription { get; }

        public ArgumentDescription CreateDescription()
        {
            return InputDescription;
        }

        DescriptionBase IDescriptionFactory.CreateDescription() =>
            CreateDescription();


        public void _Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            InputDescription.Type = new TypeReference(inputType);
        }

        public void _Type(ITypeNode type)
        {
            InputDescription.Type = InputDescription.Type
                .GetMoreSpecific(type);
        }




        public IArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            InputDescription.SyntaxNode = inputValueDefinition;
            return this;
        }

        public IArgumentDescriptor Description(string value)
        {
            InputDescription.Description = value;
            return this;
        }

        public IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            InputDescription.Type = InputDescription.Type.GetMoreSpecific(
                typeof(TInputType), TypeContext.Input);
            return this;
        }

        public IArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }
            InputDescription.Type = new TypeReference(inputType);
            return this;
        }

        public IArgumentDescriptor Type(
            ITypeNode typeNode)
        {
            InputDescription.Type = InputDescription.Type
                .GetMoreSpecific(type);
            return this;
        }

        public IArgumentDescriptor DefaultValue(IValueNode value)
        {
            InputDescription.DefaultValue =
                value ?? NullValueNode.Default;
            InputDescription.NativeDefaultValue = null;
            return this;
        }

        public IArgumentDescriptor DefaultValue(object value)
        {
            if (value == null)
            {
                InputDescription.DefaultValue = NullValueNode.Default;
                InputDescription.NativeDefaultValue = null;
            }
            else
            {
                InputDescription.Type = InputDescription.Type
                    .GetMoreSpecific(value.GetType(), TypeContext.Input);
                InputDescription.NativeDefaultValue = value;
                InputDescription.DefaultValue = null;
            }
            return this;
        }

        public IArgumentDescriptor Directive<T>(T directiveInstance) where T : class
        {
            throw new NotImplementedException();
        }

        public IArgumentDescriptor Directive<T>() where T : class, new()
        {
            throw new NotImplementedException();
        }

        public IArgumentDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
