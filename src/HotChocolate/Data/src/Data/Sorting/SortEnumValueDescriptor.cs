using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortEnumValueDescriptor
        : EnumValueDescriptor,
          ISortEnumValueDescriptor
    {
        protected SortEnumValueDescriptor(
            IDescriptorContext context,
            string? scope,
            int value)
            : base(context, new SortEnumValueDefinition {Operation = value})
        {
            ISortConvention convention = context.GetSortConvention(scope);
            Definition.Name = convention.GetOperationName(value);
            Definition.Description = convention.GetOperationDescription(value);
            Definition.Value = Definition.Name.Value;
        }

        protected SortEnumValueDescriptor(
            IDescriptorContext context,
            SortEnumValueDefinition definition)
            : base(context, definition)
        {
        }

        protected internal new EnumValueDefinition Definition
        {
            get { return base.Definition; }
            set { base.Definition = value; }
        }

        public new ISortEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition)
        {
            base.SyntaxNode(enumValueDefinition);
            return this;
        }

        public new ISortEnumValueDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortEnumValueDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortEnumValueDescriptor Deprecated(string reason)
        {
            base.Deprecated(reason);
            return this;
        }

        public new ISortEnumValueDescriptor Deprecated()
        {
            base.Deprecated();
            return this;
        }

        public new ISortEnumValueDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortEnumValueDescriptor Directive<T>() where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new ISortEnumValueDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static SortEnumValueDescriptor New(
            IDescriptorContext context,
            string? scope,
            int value) =>
            new SortEnumValueDescriptor(context, scope, value);
    }
}
