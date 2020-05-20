using System.Linq.Expressions;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters.Expressions
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
