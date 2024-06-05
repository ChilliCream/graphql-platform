using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// The base of a mongodb operation handler that can be bound to a
/// <see cref="FilterOperationField"/>. The is executed during the visitation of a input object.
/// This base is optimized to handle filter operations for mongodb
/// </summary>
public abstract class MongoDbOperationHandlerBase
    : FilterOperationHandler<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
{
    protected MongoDbOperationHandlerBase(InputParser inputParser)
    {
        InputParser = inputParser;
    }

    protected InputParser InputParser { get; }

    /// <inheritdoc/>
    public override bool TryHandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out MongoDbFilterDefinition result)
    {
        var value = node.Value;
        var runtimeType = context.RuntimeTypes.Peek();

        var type = field.Type.IsListType()
            ? runtimeType.Source.MakeArrayType()
            : runtimeType.Source;

        var parsedValue = InputParser.ParseLiteral(value, field, type);

        if ((!runtimeType.IsNullable || !CanBeNull) && parsedValue is null)
        {
            var error = ErrorHelper.CreateNonNullError(field, value, context);
            context.ReportError(error);
            result = null!;
            return false;
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
    public abstract MongoDbFilterDefinition HandleOperation(
        MongoDbFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue);
}
