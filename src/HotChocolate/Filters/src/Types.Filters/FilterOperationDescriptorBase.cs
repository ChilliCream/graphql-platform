using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters
{
    public class FilterOperationDescriptorBase
        : ArgumentDescriptorBase<FilterOperationDefintion>
    {
        protected FilterOperationDescriptorBase(
            IDescriptorContext context,
            NameString name,
            ITypeReference? type,
            FilterOperation operation,
            IFilterConvention filterConventions)
            : base(context)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
            Definition.Type = type;
            Definition.Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Definition.Description =
                filterConventions.GetOperationDescription(operation);
        }

        internal protected override FilterOperationDefintion Definition { get; } =
            new FilterOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
