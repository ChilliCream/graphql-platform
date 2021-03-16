using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortOperationInvocation
    {
        public SortOperationInvocation(
            SortOperationKind kind,
            ParameterExpression parameterExpression,
            Expression expressionBody,
            Type returnType)
        {
            Kind = kind;
            Parameter = parameterExpression
                ?? throw new ArgumentNullException(nameof(parameterExpression));
            ExpressionBody = expressionBody
                ?? throw new ArgumentNullException(nameof(expressionBody));
            ReturnType = returnType
                ?? throw new ArgumentNullException(nameof(returnType));
        }

        public SortOperationKind Kind { get; }

        public Expression ExpressionBody { get; }

        public ParameterExpression Parameter { get; }

        public Type ReturnType { get; }
    }
}
