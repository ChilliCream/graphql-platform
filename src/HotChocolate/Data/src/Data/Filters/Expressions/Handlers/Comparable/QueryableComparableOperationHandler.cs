using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    /// <summary>
    /// The base of a mongodb operation handler specific for
    /// <see cref="IComparableOperationFilterInputType "/>
    /// If the <see cref="FilterTypeInterceptor"/> encounters a operation field that implements
    /// <see cref="IComparableOperationFilterInputType "/> and matches the operation identifier
    /// defined in <see cref="QueryableComparableOperationHandler.Operation"/> the handler is bound
    /// to the field
    /// </summary>
    public abstract class QueryableComparableOperationHandler
        : QueryableOperationHandlerBase
    {
        protected QueryableComparableOperationHandler(
            ITypeConverter typeConverter)
        {
            TypeConverter = typeConverter;
        }

        /// <summary>
        /// Specifies the identifier of the operations that should be handled by this handler
        /// </summary>
        protected abstract int Operation { get; }

        protected ITypeConverter TypeConverter { get; }

        /// <summary>
        /// Checks if the <see cref="FilterField"/> implements
        /// <see cref="IComparableOperationFilterInputType "/> and has the operation identifier
        /// defined in <see cref="QueryableComparableOperationHandler.Operation"/>
        /// </summary>
        /// <param name="context">The discovery context of the schema</param>
        /// <param name="typeDefinition">The definition of the declaring type of the field</param>
        /// <param name="fieldDefinition">The definition of the field</param>
        /// <returns>Returns true if the field can be handled</returns>
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }

        /// <summary>
        /// Converts the value of the <paramref name="parsedValue"/> into the needed runtime type
        /// </summary>
        /// <param name="node">The value node to parse</param>
        /// <param name="parsedValue">The parsed value of the <paramref name="node"/></param>
        /// <param name="type">
        /// The type of the field that the <paramref name="node"/> was defined
        /// </param>
        /// <param name="context">The visitor context</param>
        /// <returns>The converted value</returns>
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
