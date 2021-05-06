using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a Equals operation field to a
    /// <see cref="Condition"/>
    /// </summary>
    public class Neo4JEqualsOperationHandler
        : Neo4JFilterOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
                fieldDefinition is FilterOperationFieldDefinition {Id: DefaultFilterOperations.Equals};

        /// <inheritdoc />
        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            Condition? expression = context
                .GetNode()
                .Property(context.GetNeo4JFilterScope().GetPath()).IsEqualTo(Cypher.LiteralOf(parsedValue));

            return expression;
        }
    }
}
