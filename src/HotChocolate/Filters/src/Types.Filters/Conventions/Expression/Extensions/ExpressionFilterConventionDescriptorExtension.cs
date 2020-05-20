using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Conventions
{
    public static class QueryableFilterConventionDescriptorExtension
    {
        public static IFilterVisitorDescriptor<Expression> UseExpressionVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            var desc = FilterVisitorDescriptor<Expression>.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
