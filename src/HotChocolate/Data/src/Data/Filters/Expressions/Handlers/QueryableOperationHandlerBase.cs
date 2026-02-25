using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Filters.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public abstract class QueryableOperationHandlerBase
    : FilterOperationHandler<QueryableFilterContext, Expression>
{
    protected QueryableOperationHandlerBase(InputParser inputParser)
    {
        InputParser = inputParser;
    }

    /// <summary>
    /// Provides access to the input parser.
    /// </summary>
    protected InputParser InputParser { get; }

    public override bool TryHandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out Expression? result)
    {
        var value = node.Value;
        var runtimeType = context.RuntimeTypes.Peek();

        var type = field.Type.IsListType()
            ? runtimeType.Source.MakeArrayType()
            : runtimeType.Source;

        var parsedValue = InputParser.ParseLiteral(value, field, type);

        if (parsedValue is null)
        {
            if (!CanBeNull)
            {
                var error = ErrorHelper.CreateNonNullError(field, value, context);
                context.ReportError(error);
                result = null!;
                return false;
            }

            if (!runtimeType.IsNullable)
            {
                if (TryCreateNullNavigationExpression(context, field, out result))
                {
                    return true;
                }

                var error = ErrorHelper.CreateNonNullError(field, value, context);
                context.ReportError(error);
                result = null!;
                return false;
            }
        }

        if (!ValueNullabilityHelpers.IsListValueValid(field.Type, runtimeType, node.Value))
        {
            var error = ErrorHelper.CreateNonNullError(field, value, context, true);
            context.ReportError(error);
            result = null!;
            return false;
        }

        result = HandleOperation(context, field, value, parsedValue);
        return true;
    }

    private static bool TryCreateNullNavigationExpression(
        QueryableFilterContext context,
        IFilterOperationField field,
        [NotNullWhen(true)] out Expression? result)
    {
        result = null;

        if (field.Id is not (DefaultFilterOperations.Equals or DefaultFilterOperations.NotEquals))
        {
            return false;
        }

        if (context.RuntimeTypes.Count < 2 || context.Scopes.Count == 0)
        {
            return false;
        }

        var parentType = context.RuntimeTypes.Skip(1).FirstOrDefault();
        var parentInstance = context.Scopes.Peek().Instance.Skip(1).FirstOrDefault();

        if (parentType is null || !parentType.IsNullable || parentInstance is null)
        {
            return false;
        }

        result = field.Id == DefaultFilterOperations.Equals
            ? FilterExpressionBuilder.Equals(parentInstance, null)
            : FilterExpressionBuilder.NotEquals(parentInstance, null);

        return true;
    }

    /// <summary>
    /// if this value is true, null values are allowed as inputs
    /// </summary>
    protected bool CanBeNull { get; set; } = true;

    /// <summary>
    /// Maps an operation field to a provider specific result.
    /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> enters a
    /// field
    /// </summary>
    /// <param name="context">The <see cref="IFilterVisitorContext{T}"/> of the visitor</param>
    /// <param name="field">The field that is currently being visited</param>
    /// <param name="value">The value node of this field</param>
    /// <param name="parsedValue">The value of the value node</param>
    /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
    public abstract Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue);
}
