using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal class ComplexFieldDescriptorBase<TDescription>
        : IDescriptionFactory<TDescription>
        where TDescription : ComplexFieldDescriptionBase
    {
        protected ComplexFieldDescriptorBase(
            ComplexFieldDescriptionBase fieldDescription)
        {
            FieldDescription = fieldDescription
                ?? throw new ArgumentNullException(nameof(fieldDescription));
        }

        protected TDescription FieldDescription { get; }

        public TDescription CreateDescription()
        {
            return FieldDescription;
        }

        DescriptionBase IDescriptionFactory.CreateDescription() =>
            CreateDescription();

        protected void SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            FieldDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            FieldDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            FieldDescription.Description = description;
        }

        protected void Type<TOutputType>()
            where TOutputType : IOutputType
        {
            FieldDescription.Type = FieldDescription
                .Type.GetMoreSpecific(
                    typeof(TOutputType),
                    TypeContext.Output);
        }

        protected void Type<TOutputType>(TOutputType type)
            where TOutputType : class, IOutputType
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            FieldDescription.Type = new TypeReference(type);
        }

        protected void Type(ITypeNode type)
        {
            FieldDescription.Type = FieldDescription
                .Type.GetMoreSpecific(type);
        }

        protected void Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            var descriptor = new ArgumentDescriptor(
                name.EnsureNotEmpty(nameof(name)));
            argument(descriptor);
            FieldDescription.Arguments.Add(descriptor.CreateDescription());
        }

        protected void DeprecationReason(string deprecationReason)
        {
            FieldDescription.DeprecationReason = deprecationReason;
        }


    }
}
