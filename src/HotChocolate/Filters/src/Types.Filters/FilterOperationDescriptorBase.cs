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

        internal protected override FilterOperationDefintion Definition { get; } =
            new FilterOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
