using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ExpressionFieldHandlers
    {
        public static IReadOnlyList<IExpressionFieldHandler> All { get; } =
           new IExpressionFieldHandler[]
           {
                new ObjectFieldHandler(),
                new ArrayFieldHandler(),
           };
    }
}
