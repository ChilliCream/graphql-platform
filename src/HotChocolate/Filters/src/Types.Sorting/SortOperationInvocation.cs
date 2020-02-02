using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationInvocation
    {
        public SortOperationInvocation(
            SortOperationKind kind,
            ParameterExpression parameterExpression,
            Expression expressionBody
        )
        {
            Kind = kind;
            Parameter = parameterExpression
                ?? throw new ArgumentNullException(nameof(parameterExpression));
            ExpressionBody = expressionBody
                ?? throw new ArgumentNullException(nameof(expressionBody));
        }

        public SortOperationKind Kind { get; }

        public Expression ExpressionBody { get; }
        public ParameterExpression Parameter { get; }
    }
}
