using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescriptorBase<T>
        : DescriptorBase<T>
        where T : ArgumentDefinition, new()
    {
        protected ArgumentDescriptorBase(IDescriptorContext context)
            : base(context)
        {
            Definition = new T();
        }

        internal protected override T Definition { get; }

        protected void SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            Definition.SyntaxNode = inputValueDefinition;
        }

        protected void Description(string value)
        {
            Definition.Description = value;
        }

        public void Type<TInputType>()
            where TInputType : IInputType
        {
            Type(typeof(TInputType));
        }

        public void Type(Type type)
        {
            Type extractedType = Context.Inspector.ExtractType(type);

            if (Context.Inspector.IsSchemaType(extractedType)
                && !typeof(IInputType).IsAssignableFrom(extractedType))
            {
                throw new ArgumentException(
                    TypeResources.ArgumentDescriptor_InputTypeViolation);
            }

            Definition.SetMoreSpecificType(
                type,
                TypeContext.Input);
        }

        public void Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (!inputType.IsInputType())
            {
                throw new ArgumentException(
                    TypeResources.ArgumentDescriptor_InputTypeViolation,
                    nameof(inputType));
            }

            Definition.Type = new SchemaTypeReference(inputType);
        }

        public void Type(ITypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }


            Definition.Type = typeReference;
        }

        public void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }

            Definition.SetMoreSpecificType(typeNode, TypeContext.Input);
        }

        public void DefaultValue(IValueNode value)
        {
            Definition.DefaultValue =
                value ?? NullValueNode.Default;
            Definition.NativeDefaultValue = null;
        }

        public void DefaultValue(object value)
        {
            if (value == null)
            {
                Definition.DefaultValue = NullValueNode.Default;
                Definition.NativeDefaultValue = null;
            }
            else
            {
                Definition.SetMoreSpecificType(
                    value.GetType(),
                    TypeContext.Input);
                Definition.NativeDefaultValue = value;
                Definition.DefaultValue = null;
            }
        }

        public void Directive<TDirective>(TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
        }

        public void Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
        }

        public void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
