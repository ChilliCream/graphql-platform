using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a NotIn operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JNotInOperationHandler
        : Neo4JFilterOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
            fieldDefinition is FilterOperationFieldDefinition { Id: NotIn };

        /// <inheritdoc />
        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            return context
                .GetNode()
                .Property(context.GetNeo4JFilterScope().GetPath())
                .In(new PlainStringLiteral(value.ToString()))
                .Not();
        }
    }
}
