using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldDescriptor
        : ArgumentDescriptorBase<FilterFieldDefinition>
        , IFilterFieldDescriptor
    {
        protected FilterFieldDescriptor(
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
            Definition.Name = convention.GetFieldName(fieldKind);
            Definition.Description = convention.GetFieldDescription(fieldKind);
            Definition.Type = convention.GetFieldType(fieldKind);
        }

        protected sealed override FilterFieldDefinition Definition { get; } =
            new FilterFieldDefinition();

        protected override void OnCreateDefinition(
            FilterFieldDefinition definition)
        {
            if (Definition.Property is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.Property);
            }

            base.OnCreateDefinition(definition);
        }

        public new IFilterFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public IFilterFieldDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IFilterFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public IFilterFieldDescriptor FieldKind(int fieldKind)
        {
            Definition.FieldKind = fieldKind;
            return this;
        }

        public new IFilterFieldDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IFilterFieldDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IFilterFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IFilterFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public new IFilterFieldDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IFilterFieldDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IFilterFieldDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IFilterFieldDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IFilterFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
