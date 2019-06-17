using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;

namespace HotChocolate.Client.Core.Builders
{
    public static class ExpressionCompiler
    {
        static ConcurrentDictionary<object, Expression> sourceExpression;

        public static bool IsUnitTesting { get; set; }

        public static T Compile<T>(Expression<T> expression)
        {
            var compiled = expression.Compile();

            if (IsUnitTesting)
            {
                if (sourceExpression == null)
                {
                    var candidate = new ConcurrentDictionary<object, Expression>();
                    Interlocked.CompareExchange(ref sourceExpression, candidate, null);
                }

                sourceExpression[compiled] = expression;
            }

            return compiled;
        }

        public static Expression GetSourceExpression(object func)
        {
            if (!IsUnitTesting)
            {
                throw new InvalidOperationException($"Cannot call {nameof(GetSourceExpression)} if {nameof(IsUnitTesting)} is false.");
            }

            return sourceExpression[func];
        }
    }
}
