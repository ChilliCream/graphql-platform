using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Expressions
{
    internal static class ExpressionOperationHandlers
    {
        public static IReadOnlyList<IExpressionOperationHandler> All { get; } =
            new IExpressionOperationHandler[]
            {
                new StringOperationHandler()
            };
    }
}
