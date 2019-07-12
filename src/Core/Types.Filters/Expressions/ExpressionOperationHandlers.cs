using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Expressions
{
    internal static class ExpressionOperationHandlers
    {
        public static IReadOnlyList<IExpressionOperationHandler> All { get; } =
            new IExpressionOperationHandler[]
            {
                new StringContainsOperationHandler(),
                new StringEndsWithOperationHandler(),
                new StringEqualsOperationHandler(),
                new StringInOperationHandler(),
                new StringStartsWithOperationHandler(),
                new BooleanOperationHandler(),
                new ComparableOperationHandler()
            };
    }
}
