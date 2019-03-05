using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescriptorBase<T>
        : DescriptorBase<T>
        where T : ArgumentDefinition, new()
    {
        protected ArgumentDescriptorBase()
        {
            Definition = new T();
        }


        protected override T Definition { get; }

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
            Definition.SetMoreSpecificType(
                typeof(TInputType),
                TypeContext.Input);
        }

        public void Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }
            Definition.Type = new SchemaTypeReference(inputType);
        }

        public void Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            Definition.SetMoreSpecificType(typeNode);
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
                Definition.Type = Definition.Type
                    .GetMoreSpecific(value.GetType(), TypeContext.Input);
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
