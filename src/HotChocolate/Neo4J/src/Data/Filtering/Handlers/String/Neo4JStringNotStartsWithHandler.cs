using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

public class Neo4JStringNotStartsWithHandler
    : Neo4JStringOperationHandler
{
    public Neo4JStringNotStartsWithHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotStartsWith;

    public override Condition HandleOperation(
        Neo4JFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is not string str)
        {
            throw new InvalidOperationException();
        }

        return context
            .GetNode()
            .Property(context.GetNeo4JFilterScope().GetPath())
            .StartsWith(Cypher.LiteralOf(str))
            .Not();
    }
}
