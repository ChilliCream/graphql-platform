using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace GreenDonut.Data.Expressions;

internal readonly struct OrderRewriterResult(
    Expression expression,
    List<LambdaExpression> orderExpressions,
    List<string> orderMethods)
{
    public Expression Expression => expression;

    public ReadOnlySpan<LambdaExpression> OrderExpressions => CollectionsMarshal.AsSpan(orderExpressions);

    public ReadOnlySpan<string> OrderMethods => CollectionsMarshal.AsSpan(orderMethods);
}
