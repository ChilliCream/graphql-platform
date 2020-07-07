using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterMethodDescriptor
        : ArgumentDescriptorBase<FilterMethodDefinition>
        , IFilterMethodDescriptor
    {
        protected FilterMethodDescriptor(
            int fieldKind,
            IDescriptorContext context,
            IFilterConvention convention)
            : base(context)
        {
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }
            Definition.FieldKind = fieldKind;
            Definition.Name = convention.GetMethodName(fieldKind);
            Definition.Description = convention.GetMethodDescription(fieldKind);
            Definition.Type = convention.GetMethodType(fieldKind);
        }

        protected sealed override FilterMethodDefinition Definition { get; } =
            new FilterMethodDefinition();

        protected override void OnCreateDefinition(
            FilterMethodDefinition definition)
        {
            if (Definition.Property is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.Property);
            }

            base.OnCreateDefinition(definition);
        }

        public new IFilterMethodDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public IFilterMethodDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IFilterMethodDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public IFilterMethodDescriptor MethodKind(int fieldKind)
        {
            Definition.MethodKind = fieldKind;
            return this;
        }

        public new IFilterMethodDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterMethodDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IFilterMethodDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IFilterMethodDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IFilterMethodDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public new IFilterMethodDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IFilterMethodDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IFilterMethodDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IFilterMethodDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IFilterMethodDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }

}
