using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescriptor
        : ArgumentDescriptorBase<ArgumentDefinition>
        , IArgumentDescriptor
    {
        protected internal ArgumentDescriptor(
            IDescriptorContext context,
            NameString argumentName)
            : base(context)
        {
            Definition.Name = argumentName.EnsureNotEmpty(nameof(argumentName));
        }

        protected internal ArgumentDescriptor(
            IDescriptorContext context,
            NameString argumentName,
            Type argumentType)
            : this(context, argumentName)
        {
            if (argumentType is null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            Definition.Name = argumentName;
            Definition.Type = context.TypeInspector.GetTypeRef(argumentType, TypeContext.Input);
        }

        protected internal ArgumentDescriptor(
            IDescriptorContext context,
            ParameterInfo parameter)
            : base(context)
        {
            Definition.Name = context.Naming.GetArgumentName(parameter);
            Definition.Description = context.Naming.GetArgumentDescription(parameter);
            Definition.Type = context.TypeInspector.GetArgumentTypeRef(parameter);
            Definition.Parameter = parameter;

            if (context.TypeInspector.TryGetDefaultValue(parameter, out object defaultValue))
            {
                Definition.NativeDefaultValue = defaultValue;
            }
        }

        protected internal ArgumentDescriptor(
            IDescriptorContext context,
            ArgumentDefinition definition)
            : base(context)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected override void OnCreateDefinition(ArgumentDefinition definition)
        {
            if (!Definition.AttributesAreApplied && Definition.Parameter is not null)
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.Parameter);
                Definition.AttributesAreApplied = true;
            }

            base.OnCreateDefinition(definition);
        }

        public new IArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public new IArgumentDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IArgumentDescriptor Type(
            ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IArgumentDescriptor Type(
            Type type)
        {
            base.Type(type);
            return this;
        }

        public new IArgumentDescriptor DefaultValue(IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IArgumentDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IArgumentDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IArgumentDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IArgumentDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static ArgumentDescriptor New(
            IDescriptorContext context,
            NameString argumentName) =>
            new ArgumentDescriptor(context, argumentName);

        public static ArgumentDescriptor New(
            IDescriptorContext context,
            NameString argumentName,
            Type argumentType) =>
            new ArgumentDescriptor(context, argumentName, argumentType);

        public static ArgumentDescriptor New(
            IDescriptorContext context,
            ParameterInfo parameter) =>
            new ArgumentDescriptor(context, parameter);

        public static ArgumentDescriptor From(
            IDescriptorContext context,
            ArgumentDefinition argumentDefinition) =>
            new ArgumentDescriptor(context, argumentDefinition);
    }
}
