using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionOperationDescriptor : FilterConventionOperationDescriptorBase,
        IFilterConventionOperationDescriptor
    {
        private readonly FilterConventionTypeDescriptor _descriptor;

        protected FilterConventionOperationDescriptor(
            FilterConventionTypeDescriptor descriptor,
            FilterOperationKind kind) : base(kind)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Definition.OperationKind = kind;
        }

        public IFilterConventionTypeDescriptor And()
        {
            return _descriptor;
        }

        public new IFilterConventionOperationDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }


        public new IFilterConventionOperationDescriptor Name(CreateFieldName factory)
        {
            base.Name(factory);
            return this;
        }

        public new IFilterConventionOperationDescriptor Ignore(bool ignore = true)
        {
            base.Ignore(ignore);
            return this;
        }

        public static FilterConventionOperationDescriptor New(
            FilterConventionTypeDescriptor descriptor, FilterOperationKind kind) =>
                new FilterConventionOperationDescriptor(descriptor, kind);
    }
}
