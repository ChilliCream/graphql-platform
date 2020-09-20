using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldDescriptor
        : ArgumentDescriptorBase<FilterFieldDefinition>
        , IFilterFieldDescriptor
    {
        protected FilterFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            NameString fieldName)
            : base(context)
        {
            Definition.Name = fieldName.EnsureNotEmpty(nameof(fieldName));
            Definition.Scope = scope;
        }

        protected FilterFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            MemberInfo member)
            : base(context)
        {
            IFilterConvention? convention = context.GetFilterConvention(scope);

            Definition.Member = member ??
                throw new ArgumentNullException(nameof(member));

            Definition.Name = convention.GetFieldName(member);
            Definition.Description = convention.GetFieldDescription(member);
            Definition.Type = convention.GetFieldType(member);
            Definition.Scope = scope;
        }

        protected internal new FilterFieldDefinition Definition
        {
            get => base.Definition;
            protected set => base.Definition = value;
        }

        internal InputFieldDefinition CreateFieldDefinition() => CreateDefinition();

        protected override void OnCreateDefinition(
            FilterFieldDefinition definition)
        {
            if (Definition.Member is { })
            {
                Context.TypeInspector.ApplyAttributes(Context, this, Definition.Member);
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

        public static FilterFieldDescriptor New(
            IDescriptorContext context,
            string? scope,
            MemberInfo member) =>
            new FilterFieldDescriptor(context, scope, member);

        public static FilterFieldDescriptor New(
            IDescriptorContext context,
            NameString fieldName,
            string? scope) =>
            new FilterFieldDescriptor(context, scope, fieldName);
    }
}
