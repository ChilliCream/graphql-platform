namespace HotChocolate.Types.Filters.Conventions
{
    public static class QueryableFilterConventionDescriptorExtension
    {
        public static IFilterExpressionVisitorDescriptor UseExpressionVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            var desc = FilterExpressionVisitorDescriptor.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
