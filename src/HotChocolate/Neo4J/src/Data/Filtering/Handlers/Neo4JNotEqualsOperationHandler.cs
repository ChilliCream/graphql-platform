using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using static HotChocolate.Data.Filters.DefaultFilterOperations;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering;

/// <summary>
/// This filter operation handler maps a NotEquals operation field to a
/// <see cref="FilterDefinition{TDocument}"/>
/// </summary>
public class Neo4JNotEqualsOperationHandler
    : Neo4JFilterOperationHandlerBase
{
    public Neo4JNotEqualsOperationHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        return fieldDefinition is FilterOperationFieldDefinition { Id: NotEquals };
    }

    /// <inheritdoc />
    public override Condition HandleOperation(
        Neo4JFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var expression = context
            .GetNode()
            .Property(context.GetNeo4JFilterScope().GetPath())
            .IsNotEqualTo(Cypher.LiteralOf(parsedValue));

        return expression;
    }
}