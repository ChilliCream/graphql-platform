using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ExpressionFieldHandlers
    {
        public static IReadOnlyList<IExpressionFieldHandler> All { get; } =
           new IExpressionFieldHandler[]
           {
                new ObjectFieldHandler(),
                new ArrayFieldHandler(),
           };
    }
}
