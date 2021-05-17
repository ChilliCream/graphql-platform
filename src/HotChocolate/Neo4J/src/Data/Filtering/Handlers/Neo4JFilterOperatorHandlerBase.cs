using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// The base of a Neo4j operation handler that can be bound to a
    /// <see cref="FilterOperationField"/>. This is executed during the visitation of a input
    /// object. This base is optimized to handle filter operations for Neo4j
    /// </summary>
    public abstract class Neo4JFilterOperationHandlerBase
        : FilterOperationHandler<Neo4JFilterVisitorContext, Condition>
    {
        /// <inheritdoc/>
        public override bool TryHandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out Condition result)
        {
            IValueNode value = node.Value;
            object? parsedValue = field.Type.ParseLiteral(value);

            if ((!context.RuntimeTypes.Peek().IsNullable || !CanBeNull) &&
                parsedValue is null)
            {
                context.ReportError(ErrorHelper.CreateNonNullError(field, value, context));

                result = null!;
                return false;
            }

            if (!field.Type.IsInstanceOfType(value))
            {
                throw new InvalidOperationException();
            }

            result = HandleOperation(context, field, value, parsedValue);

            return true;
        }

        /// <summary>
        /// if this value is true, null values are allowed as inputs
        /// </summary>
        protected bool CanBeNull { get; set; } = true;

        /// <summary>
        /// Maps a operation field to a provider specific result.
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> enters a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="IFilterVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="value">The value node of this field</param>
        /// <param name="parsedValue">The value of the value node</param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        public abstract Condition HandleOperation(
            Neo4JFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue);
    }
}
