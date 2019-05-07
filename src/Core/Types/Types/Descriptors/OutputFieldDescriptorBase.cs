using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using NJsonSchema.Infrastructure;

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

            var descriptor = new ArgumentDescriptor(
                Context,
                name.EnsureNotEmpty(nameof(name)));
            var defaultDescription = string.Empty;
            MemberInfo memberDetails = null;

            switch (Definition)
            {
                case ObjectFieldDefinition objectFieldDefinition:
                    memberDetails = objectFieldDefinition.Member;
                    break;
                case InterfaceFieldDefinition interfaceDefinition:
                    memberDetails = interfaceDefinition.Member;
                    break;
            }

            if (memberDetails != null && (memberDetails.MemberType & MemberTypes.Method) != 0)
            {
                MethodInfo m = memberDetails.DeclaringType.GetMethod(memberDetails.Name);
                ParameterInfo param = m.GetParameters().SingleOrDefault(p => p.Name == name);

                if (param != null)
                {
                    defaultDescription = param.GetXmlSummary();
                }
            }

            descriptor.Description(defaultDescription);
            argument(descriptor);
            Definition.Arguments.Add(descriptor.CreateDefinition());
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
