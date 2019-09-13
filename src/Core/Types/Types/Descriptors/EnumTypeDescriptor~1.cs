using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class EnumTypeDescriptor<T>
        : EnumTypeDescriptor
        , IEnumTypeDescriptor<T>
    {
        protected internal EnumTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
        }

        public new IEnumTypeDescriptor<T> SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition)
        {
            base.SyntaxNode(enumTypeDefinition);
            return this;
        }

        public new IEnumTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IEnumTypeDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IEnumTypeDescriptor<T> BindItems(
            BindingBehavior behavior) =>
            BindValues(behavior);

        public new IEnumTypeDescriptor<T> BindValues(BindingBehavior behavior)
        {
            base.BindValues(behavior);
            return this;
        }

        public new IEnumTypeDescriptor<T> BindValuesExplicitly() =>
            BindValues(BindingBehavior.Explicit);

        public new IEnumTypeDescriptor<T> BindValuesImplicitly() =>
            BindValues(BindingBehavior.Implicit);

        public IEnumValueDescriptor Item(T value)
        {
            return base.Item(value);
        }

        public IEnumValueDescriptor Value(T value) => Item(value);

        public new IEnumTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IEnumTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
