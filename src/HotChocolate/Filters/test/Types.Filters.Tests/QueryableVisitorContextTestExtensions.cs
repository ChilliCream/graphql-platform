using System;
using System.Linq.Expressions;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public static class QueryableVisitorContextTestExtensions
    {
        public static Expression<Func<TSource, bool>> CreateOrAssert<TSource>(
            this QueryableFilterVisitorContext context)
        {
            if (context.GetClosure().TryCreateLambda(
                out Expression<Func<TSource, bool>> expression) &&
                expression is { })
            {
                return expression;
            }

            Assert.True(false, "Filter could not be created");
            return expression;
        }
    }
}
