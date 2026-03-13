using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class ExecutorBuilder
{
    private readonly IFilterInputType _inputType;

    public ExecutorBuilder(IFilterInputType inputType)
    {
        _inputType = inputType;
    }

    public Func<T, bool> Build<T>(IValueNode filter)
    {
        return BuildExpression<T>(filter).Compile();
    }

    public Expression<Func<T, bool>> BuildExpression<T>(IValueNode filter)
    {
        var visitorContext = new QueryableFilterContext(_inputType, true);
        var visitor = new FilterVisitor<QueryableFilterContext, Expression>(
            new QueryableCombinator());

        visitor.Visit(filter, visitorContext);

        if (visitorContext.TryCreateLambda(out Expression<Func<T, bool>>? where))
        {
            return where;
        }

        throw new InvalidOperationException();
    }
}
