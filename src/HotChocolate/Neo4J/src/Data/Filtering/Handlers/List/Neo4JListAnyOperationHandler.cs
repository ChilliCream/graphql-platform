using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.Neo4J.Filtering
{
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
                fieldDefinition is FilterOperationFieldDefinition { Id: Any };
        }

        /// <inheritdoc />
        public override Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (context.RuntimeTypes.Count > 0 &&
                context.RuntimeTypes.Peek().TypeArguments is { Count: > 0 } &&
                parsedValue is bool parsedBool &&
                context.Scopes.Peek() is Neo4JFilterScope)
            {
                if (parsedBool)
                {
                    return new CompoundCondition(Operator.And);
                }

                return new CompoundCondition(Operator.And);
            }

            throw new InvalidOperationException();
        }
    }
}
