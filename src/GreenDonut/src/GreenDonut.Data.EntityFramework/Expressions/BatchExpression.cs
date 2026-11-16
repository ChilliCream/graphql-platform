using System.Linq.Expressions;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data.Expressions;

internal readonly struct BatchExpression<TK, TV>(
    Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> selectExpression,
    bool isBackward,
    Cursor? cursor)
{
    public Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> SelectExpression { get; } = selectExpression;
    public bool IsBackward { get; } = isBackward;
    public Cursor? Cursor { get; } = cursor;
}
