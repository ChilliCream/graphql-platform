using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
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
