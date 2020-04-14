
namespace HotChocolate.Types.Sorting.Conventions
{
    public static class QueryableSortingConventionDescriptorExtension
    {
        public static ISortingExpressionVisitorDescriptor UseExpressionVisitor(
            this ISortingConventionDescriptor descriptor)
        {
            var desc = SortingExpressionVisitorDescriptor.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
