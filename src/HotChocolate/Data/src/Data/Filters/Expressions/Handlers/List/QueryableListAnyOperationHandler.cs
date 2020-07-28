using System;
using System.Linq.Expressions;
using System.Reflection;
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
            TypeConverter = typeConverter ?? DefaultTypeConverter.Default;
        }

        protected ITypeConverter TypeConverter { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Operation == Operations.Any;
        }

        public override Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue)
        {
            if (context.TryGetDeclaringField(out IFilterField? parentField) &&
                parentField.ElementType is { } &&
                parsedValue is bool parsedBool)
            {
                Expression? property = context.GetInstance();

                Expression expression;
                if (parsedBool)
                {
                    expression = FilterExpressionBuilder.Any(
                        parentField.ElementType,
                        property);
                }
                else
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.Any(
                            parentField.ElementType,
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
