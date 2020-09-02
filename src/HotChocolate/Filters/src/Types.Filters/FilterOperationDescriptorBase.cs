using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class FilterOperationDescriptorBase
        : ArgumentDescriptorBase<FilterOperationDefintion>
    {
        protected FilterOperationDescriptorBase(
            IDescriptorContext context)
            : base(context)
        {
        }

        protected internal override FilterOperationDefintion Definition { get; protected set; } =
            new FilterOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
