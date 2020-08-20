using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableListAnyOperationHandler
        : QueryableOperationHandlerBase
    {
        public QueryableListAnyOperationHandler(
            ITypeConverter typeConverter)
        {
            TypeConverter = typeConverter;
            CanBeNull = false;
        }

        protected ITypeConverter TypeConverter { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == DefaultOperations.Any;
        }

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue)
        {
            if (context.TypeInfos.Count > 0 && 
                context.TypeInfos.Peek().TypeArguments.FirstOrDefault() is FilterTypeInfo element &&
                parsedValue is bool parsedBool)
            {
                Expression? property = context.GetInstance();

                Expression expression;
                if (parsedBool)
                {
                    expression = FilterExpressionBuilder.Any(
                        element.Type,
                        property);
                }
                else
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.Any(
                            element.Type,
                            property));
                }

                if (context.InMemory)
                {
                    expression =
                        FilterExpressionBuilder.NotNullAndAlso(property, expression);
                }

                return expression;
            }
            throw new InvalidOperationException();
        }
    }
}
