using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor<T>
        : FilterInputTypeDescriptor
        , IFilterInputTypeDescriptor<T>
    {
        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention)
            : base(context, entityType, convention)
        {
        }

        public new IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive(new TDirective());
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        public TDesc AddFilter<TDesc>(
            PropertyInfo property,
            Func<IDescriptorContext, TDesc> factory)
            where TDesc : FilterFieldDescriptorBase =>
                Fields.GetOrAddDescriptor(property, () => factory(Context));

        public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention) =>
                new FilterInputTypeDescriptor<T>(context, entityType, convention);
    }
}
