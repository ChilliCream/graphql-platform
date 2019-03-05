using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public abstract class OutputFieldDescriptorBase<TDescription>
        : DescriptorBase<TDescription>
        where TDescription : OutputFieldDefinitionBase
    {
        protected void SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            Definition.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            Definition.Description = description;
        }

        protected void Type<TOutputType>()
            where TOutputType : IOutputType
        {
            Definition.SetMoreSpecificType(
                typeof(TOutputType),
                TypeContext.Output);
        }

        protected void Type<TOutputType>(TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            if (outputType == null)
            {
                throw new ArgumentNullException(nameof(outputType));
            }
            Definition.Type = new SchemaTypeReference(outputType);
        }

        protected void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode);
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
            Definition.Arguments.Add(descriptor.CreateDefinition());
        }

        protected void DeprecationReason(string deprecationReason)
        {
            Definition.DeprecationReason = deprecationReason;
        }

        protected void Ignore()
        {
            Definition.Ignore = true;
        }

        protected void Directive<T>(T directive)
            where T : class
        {
            Definition.AddDirective(directive);
        }

        protected void Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
        }

        protected void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
