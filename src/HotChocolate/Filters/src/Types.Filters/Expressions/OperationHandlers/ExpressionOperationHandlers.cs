using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ExpressionOperationHandlers
    {
        public static IReadOnlyList<IExpressionOperationHandler> All { get; } =
            new IExpressionOperationHandler[]
            {
                new StringContainsOperationHandler(),
                new StringEndsWithOperationHandler(),
                new StringEqualsOperationHandler(),
                new StringInOperationHandler(),
                new StringStartsWithOperationHandler(),
                new ComparableEqualsOperationHandler(),
                new ComparableGreaterThanOperationHandler(),
                new ComparableGreaterThanOrEqualsOperationHandler(),
                new ComparableLowerThanOperationHandler(),
                new ComparableLowerThanOrEqualsOperationHandler(),
                new ComparableInOperationHandler(),
                new BooleanEqualsOperationHandler(),
                new ArrayAnyOperationHandler(),
            };
    }
}
