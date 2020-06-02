using System.Linq.Expressions;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class QueryableFilterConventionDescriptorExtension
    {
        public static IFilterVisitorDescriptor<Expression> UseExpressionVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor.TryGetVisitor(out FilterVisitorDescriptorBase? visitor) &&
                visitor is FilterVisitorDescriptor<Expression> expressionVisitor)
            {
                return expressionVisitor;
            }

            var desc = FilterVisitorDescriptor<Expression>.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
