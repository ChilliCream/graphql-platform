using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterOperationDescriptorBase
        : FilterOperationDescriptorBase
        , IBooleanFilterOperationDescriptorBase
    {
        protected BooleanFilterOperationDescriptorBase(
            IDescriptorContext context,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context, name, type, operation, filterConventions)
        { 
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptorBase Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptorBase Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptorBase Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptorBase Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new IBooleanFilterOperationDescriptorBase Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
