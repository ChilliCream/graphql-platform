using System;
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

        protected internal override T Definition { get; protected set; }

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
            var typeInfo = Context.TypeInspector.CreateTypeInfo(type);

            if (typeInfo.IsSchemaType && !typeInfo.IsInputType())
            {
                throw new ArgumentException(
                    TypeResources.ArgumentDescriptor_InputTypeViolation);
            }

            Definition.SetMoreSpecificType(typeInfo.GetExtendedType(), TypeContext.Input);
        }

        public void Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType
        {
            if (inputType is null)
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
            if (typeReference is null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }


            Definition.Type = typeReference;
        }

        public void Type(ITypeNode typeNode)
        {
            if (typeNode is null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }

            Definition.SetMoreSpecificType(typeNode, TypeContext.Input);
        }

        public void DefaultValue(IValueNode value)
        {
            Definition.DefaultValue = value ?? NullValueNode.Default;
            Definition.NativeDefaultValue = null;
        }

        public void DefaultValue(object value)
        {
            if (value is null)
            {
                Definition.DefaultValue = NullValueNode.Default;
                Definition.NativeDefaultValue = null;
            }
            else
            {
                Definition.SetMoreSpecificType(
                    Context.TypeInspector.GetType(value.GetType()),
                    TypeContext.Input);
                Definition.NativeDefaultValue = value;
                Definition.DefaultValue = null;
            }
        }

        public void Directive<TDirective>(TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
        }

        public void Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective(), Context.TypeInspector);
        }

        public void Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
        }
    }
}
