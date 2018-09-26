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

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            FieldDescription.Name = name;
        }

        protected void Description(string description)
        {
            FieldDescription.Description = description;
        }

        protected void Type<TOutputType>() where TOutputType : IOutputType
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(typeof(TOutputType));
        }

        protected void Type(ITypeNode type)
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(type);
        }

        protected void Argument(string name, Action<IArgumentDescriptor> argument)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The argument name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL argument name.",
                    nameof(name));
            }

            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            var descriptor = new ArgumentDescriptor(name);
            argument(descriptor);
            FieldDescription.Arguments.Add(descriptor.CreateDescription());
        }

        protected void DeprecationReason(string deprecationReason)
        {
            FieldDescription.DeprecationReason = deprecationReason;
        }
    }
}
