using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using System.Reflection;
using System.Globalization;

namespace HotChocolate.Types.Descriptors
{
    public abstract class OutputFieldDescriptorBase<TDefinition>
        : DescriptorBase<TDefinition>
        where TDefinition : OutputFieldDefinitionBase
    {
        protected OutputFieldDescriptorBase(IDescriptorContext context)
            : base(context)
        {
        }

        protected IReadOnlyDictionary<NameString, ParameterInfo> Parameters
        { get; set; }

        protected void SyntaxNode(
            FieldDefinitionNode syntaxNode)
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
            Type type = Context.Inspector.ExtractType(typeof(TOutputType));
            if (Context.Inspector.IsSchemaType(type)
                && !typeof(IOutputType).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    TypeResources.ObjectFieldDescriptorBase_FieldType);
            }

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

            if (!outputType.IsOutputType())
            {
                throw new ArgumentException(
                    TypeResources.ObjectFieldDescriptorBase_FieldType);
            }

            Definition.Type = new SchemaTypeReference(outputType);
        }

        protected void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode, TypeContext.Output);
        }

        protected void Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            name.EnsureNotEmpty(nameof(name));

            ArgumentDescriptor descriptor =
                Parameters != null
                && Parameters.TryGetValue(name, out ParameterInfo p)
                    ? ArgumentDescriptor.New(Context, p)
                    : ArgumentDescriptor.New(Context, name);

            argument(descriptor);

            ArgumentDefinition definition = descriptor.CreateDefinition();
            Definition.Arguments.Add(definition);
        }

        protected void DeprecationReason(string reason)
        {
            Definition.DeprecationReason = reason;
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
