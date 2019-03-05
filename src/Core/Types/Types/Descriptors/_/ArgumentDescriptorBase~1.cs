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
            Definition.Type = Definition.Type.GetMoreSpecific(
                typeof(TInputType), TypeContext.Input);
        }

        public void Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }
            Definition.Type = new SchemaTypeReference(inputType);
        }

        public void Type(
            ITypeNode typeNode)
        {
            Definition.Type = Definition.Type
                .GetMoreSpecific(typeNode);
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

        public void Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
        }

        public void Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
        }

        public void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
