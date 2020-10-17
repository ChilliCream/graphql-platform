using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableComparableOperationHandler
        : QueryableOperationHandlerBase
    {
        protected QueryableComparableOperationHandler(
            ITypeConverter typeConverter)
        {
            TypeConverter = typeConverter;
        }

        protected abstract int Operation { get; }

        protected ITypeConverter TypeConverter { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationFilterInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }

        protected object? ParseValue(
            IValueNode node,
            object? parsedValue,
            IType type,
            QueryableFilterContext context)
        {
            if (parsedValue is null)
            {
                return parsedValue;
            }

            Type returnType = context.RuntimeTypes.Peek().Source;

            if (type.IsListType())
            {
                Type elementType = type.ElementType().ToRuntimeType();

                if (returnType != elementType)
                {
                    Type listType = typeof(List<>).MakeGenericType(returnType);
                    parsedValue = TypeConverter.Convert(typeof(object), listType, parsedValue) ??
                        throw ThrowHelper.FilterConvention_CouldNotConvertValue(node);
                }

                return parsedValue;
            }

            if (!returnType.IsInstanceOfType(parsedValue))
            {
                parsedValue = TypeConverter.Convert(typeof(object), returnType, parsedValue) ??
                    throw ThrowHelper.FilterConvention_CouldNotConvertValue(node);
            }

            return parsedValue;
        }
    }
}
