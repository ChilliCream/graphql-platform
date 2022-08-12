using System.Linq.Expressions;

namespace HotChocolate.Data.Marten;

public static class MartenExpressionTranslator
{
    public static Expression TranslateFilterExpression(Expression filterExpression)
    {
        if (filterExpression is BinaryExpression
                {
                    NodeType: ExpressionType.AndAlso,
                    Left: BinaryExpression leftBinaryExpression,
                    Right: BinaryExpression rightBinaryExpression
                })
        {
            if (ReferenceComparisonExpressionShouldBeRemoved(leftBinaryExpression)
                && rightBinaryExpression.Left is MemberExpression)
            {
                return rightBinaryExpression;
            }
            else if (ReferenceComparisonExpressionShouldBeRemoved(rightBinaryExpression)
                     && leftBinaryExpression.Left is MemberExpression)
            {
                return leftBinaryExpression;
            }
        }
        return filterExpression;
    }

    private static bool ReferenceComparisonExpressionShouldBeRemoved(BinaryExpression expression)
    {
        return expression.NodeType == ExpressionType.NotEqual
               && expression.Method == null
               && !expression.Left.Type.IsValueType && !expression.Right.Type.IsValueType;
    }

    public static Expression TranslateSortExpression(Expression sortExpression)
    {
        if (sortExpression is ConditionalExpression
                {IfFalse: MemberExpression memberExpression})
        {
            return memberExpression;
        }
        return sortExpression;
    }
}
