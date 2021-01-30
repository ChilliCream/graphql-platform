using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a In operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JInOperationHandler
        : Neo4JFilterOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                   operationField.Id is DefaultFilterOperations.In;
        }

        /// <inheritdoc />
        public override Neo4JFilterDefinition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var doc = new Neo4JFilterOperation(Operator.In.GetRepresentation(), parsedValue);

            return new Neo4JFilterOperation(context.GetNeo4JFilterScope().GetPath(), doc);
        }
    }
}
