using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions;

/// <summary>
/// Defines meta that for a filter field that the provider can use to build the database query
/// </summary>
public class ExpressionFilterMetadata : IFilterMetadata
{
    public ExpressionFilterMetadata(Expression? expression)
    {
        Expression = expression;
    }

    public Expression? Expression { get; }
}
