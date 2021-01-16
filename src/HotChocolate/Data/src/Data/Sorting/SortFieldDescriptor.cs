using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortFieldDescriptor
        : ArgumentDescriptorBase<SortFieldDefinition>
        , ISortFieldDescriptor
    {
        protected SortFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            NameString fieldName)
            : base(context)
        {
            Definition.Name = fieldName.EnsureNotEmpty(nameof(fieldName));
            Definition.Scope = scope;
        }

        protected SortFieldDescriptor(
            IDescriptorContext context,
            string? scope,
            MemberInfo member)
            : base(context)
        {
            ISortConvention? convention = context.GetSortConvention(scope);

            Definition.Member = member ??
                throw new ArgumentNullException(nameof(member));

            Definition.Name = convention.GetFieldName(member);
            Definition.Description = convention.GetFieldDescription(member);
            Definition.Type = convention.GetFieldType(member);
            Definition.Scope = scope;
        }

        protected internal SortFieldDescriptor(
            IDescriptorContext context,
            string? scope)
            : base(context)
        {
            Definition.Scope = scope;
        }

        protected internal new SortFieldDefinition Definition
        {
            get => base.Definition;
            protected set => base.Definition = value;
        }

        internal InputFieldDefinition CreateFieldDefinition() => CreateDefinition();

        protected override void OnCreateDefinition(
            SortFieldDefinition definition)
        {
            if (Definition.Member is { })
            {
                Context.TypeInspector.ApplyAttributes(Context, this, Definition.Member);
            }

            base.OnCreateDefinition(definition);
        }

        public new ISortFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public ISortFieldDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public ISortFieldDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public new ISortFieldDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new ISortFieldDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new ISortFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new ISortFieldDescriptor Type(Type type)
        {
            base.Type(type);
            return this;
        }

        public new ISortFieldDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new ISortFieldDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new ISortFieldDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortFieldDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new ISortFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static SortFieldDescriptor New(
            IDescriptorContext context,
            string? scope,
            MemberInfo member) =>
            new SortFieldDescriptor(context, scope, member);

        public static SortFieldDescriptor New(
            IDescriptorContext context,
            NameString fieldName,
            string? scope) =>
            new SortFieldDescriptor(context, scope, fieldName);
    }
}
