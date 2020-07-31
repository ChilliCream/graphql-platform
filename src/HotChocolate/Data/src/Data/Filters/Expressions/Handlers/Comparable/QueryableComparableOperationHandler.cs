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
        public QueryableComparableOperationHandler(
            ITypeConverter typeConverter)
        {
            TypeConverter = typeConverter ?? DefaultTypeConverter.Default;
        }

        protected abstract int Operation { get; }

        protected ITypeConverter TypeConverter { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Operation == Operation;
        }

        protected object ParseValue(
            IValueNode node,
            object parsedValue,
            IType type,
            QueryableFilterContext context)
        {
            Type? returnType = context.ClrTypes.Peek();

            if (type.IsListType())
            {
                Type elementType = type.ElementType().ToClrType();

                if (returnType != elementType)
                {
                    Type listType = typeof(List<>).MakeGenericType(
                        returnType);

                    parsedValue = TypeConverter.Convert(
                        typeof(object),
                        listType,
                        parsedValue) ??
                        throw ThrowHelper.FilterConvention_CouldNotConvertValue(node);
                }

                return parsedValue;
            }
            else
            {
                if (!returnType.IsInstanceOfType(parsedValue))
                {
                    parsedValue = TypeConverter.Convert(
                        typeof(object),
                        returnType,
                        parsedValue) ??
                        throw ThrowHelper.FilterConvention_CouldNotConvertValue(node);
                }

                return parsedValue;
            }
        }
    }
}
