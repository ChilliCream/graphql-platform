using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// This filter operation handler maps a Any operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class Neo4JListAnyOperationHandler
        : Neo4JFilterOperationHandlerBase
    {
        public Neo4JListAnyOperationHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.Any;
        }

        /// <inheritdoc />
        public override Neo4JFilterDefinition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (context.RuntimeTypes.Count <= 0 || context.RuntimeTypes.Peek().TypeArguments is not {Count: > 0} ||
                parsedValue is not bool parsedBool ||
                context.Scopes.Peek() is not Neo4JFilterScope scope) throw new InvalidOperationException();
            var path = scope.GetPath();

            if (parsedBool)
            {
                // TODO: Implement ListAnyOperationHandler
                return new Neo4JFilterOperation(
                    path,
                    null);
            }

            // TODO: Implement ListAnyOperationHandler
            return new Neo4JFilterOperation(
                path,
                null);
        }
    }
}
