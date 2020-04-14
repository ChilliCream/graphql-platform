using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public static class SortQueryableClosureExtensions
    {
        public static SortOperationInvocation CreateSortOperation(
            this SortQueryableClosure closure,
            SortOperationKind kind)
        {
            return new SortOperationInvocation(
                kind,
                closure.Parameter,
                closure.Instance.Peek(),
                closure.Property.Peek().PropertyType);
        }

        public static SortOperationInvocation CreateInMemorySortOperation(
            this SortQueryableClosure closure,
            SortOperationKind kind)
        {
            Expression nextExpression = closure.Instance.Peek();
            if (closure.Property.Count > 0)
            {
                DefaultExpression defaultOfType =
                    Expression.Default(closure.Property.Peek().PropertyType);
                Stack<Expression>.Enumerator enumerator = closure.Instance.GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    nextExpression =
                        SortExpressionBuilder.IfNullThenDefault(
                            enumerator.Current, nextExpression, defaultOfType);
                }
            }
            return new SortOperationInvocation(
                kind,
                closure.Parameter,
                nextExpression,
                closure.Property.Peek().PropertyType);
        }
    }
}
