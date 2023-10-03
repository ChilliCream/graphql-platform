using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

/// <summary>
/// This filter operation handler maps a NotLowerThanOrEquals operation field to a
/// <see cref="FilterDefinition{TDocument}"/>
/// </summary>
public class Neo4JComparableNotLowerThanOrEqualsHandler
    : Neo4JComparableOperationHandler
{
    public Neo4JComparableNotLowerThanOrEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.NotLowerThanOrEquals;

    /// <inheritdoc />
    public override Condition HandleOperation(
        Neo4JFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is null)
        {
            throw new InvalidOperationException();
        }

        return context
            .GetNode()
            .Property(context.GetNeo4JFilterScope().GetPath())
            .LessThanOrEqualTo(Cypher.LiteralOf(parsedValue))
            .Not();
    }
}
