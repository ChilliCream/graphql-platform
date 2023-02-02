using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions;

/// <summary>
/// Defines meta that for a sort field that the provider can use to build the database query
/// </summary>
public class ExpressionSortMetadata : ISortMetadata
{
    public ExpressionSortMetadata(Expression? expression)
    {
        Expression = expression;
    }

    public Expression? Expression { get; }
}
