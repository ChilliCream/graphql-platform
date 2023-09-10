using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

/// <summary>
/// This filter operation handler maps a NotGreaterThanOrEquals operation field to a
/// <see cref="FilterDefinition{TDocument}"/>
/// </summary>
public class Neo4JComparableNotGreaterThanOrEqualsHandler
    : Neo4JComparableOperationHandler
{
    public Neo4JComparableNotGreaterThanOrEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.NotGreaterThanOrEquals;

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
            .GreaterThanOEqualTo(Cypher.LiteralOf(parsedValue))
            .Not();
    }
}
