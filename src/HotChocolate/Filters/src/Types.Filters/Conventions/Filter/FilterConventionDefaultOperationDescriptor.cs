using System;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefaultOperationDescriptor
        : FilterConventionOperationDescriptorBase
        , IFilterConventionDefaultOperationDescriptor
    {
        private readonly FilterConventionDescriptor _descriptor;

        protected FilterConventionDefaultOperationDescriptor(
            FilterConventionDescriptor descriptor,
            FilterOperationKind kind) : base(kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.OperationKind = kind;
        }

        public IFilterConventionDescriptor And()
        {
            return _descriptor;
        }

        public new IFilterConventionDefaultOperationDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterConventionDefaultOperationDescriptor Name(CreateFieldName factory)
        {
            base.Name(factory);
            return this;
        }

        public static FilterConventionDefaultOperationDescriptor New(
            FilterConventionDescriptor descriptor, FilterOperationKind kind) =>
            new FilterConventionDefaultOperationDescriptor(descriptor, kind);
    }
}
