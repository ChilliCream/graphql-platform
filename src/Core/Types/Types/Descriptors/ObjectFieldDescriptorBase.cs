using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescriptorBase
        : IDescriptionFactory<ObjectFieldDescriptionBase>
    {
        protected ObjectFieldDescriptorBase(
            ObjectFieldDescriptionBase fieldDescription)
        {
            FieldDescription = fieldDescription
                ?? throw new ArgumentNullException(nameof(fieldDescription));
        }

        protected ObjectFieldDescriptionBase FieldDescription { get; }

        public ObjectFieldDescriptionBase CreateDescription()
        {
            return FieldDescription;
        }

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

        protected void Type<TOutputType>() where TOutputType : IOutputType
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(
                    typeof(TOutputType),
                    TypeContext.Output);
        }

        protected void Type(ITypeNode type)
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(type);
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
