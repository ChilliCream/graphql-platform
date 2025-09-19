using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

public static class HotChocolateExecutionRequestContextExtensions
{
    public static bool TryGetOperationDefinition(
        this RequestContext context,
        [NotNullWhen(true)] out OperationDefinitionNode? operationDefinition)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();

        if (operationInfo.Operation is not null)
        {
            operationDefinition = operationInfo.Operation.Definition;
            return true;
        }

        if (operationInfo.Definition is not null)
        {
            operationDefinition = operationInfo.Definition;
            return true;
        }

        operationDefinition = null;
        return false;
    }

    public static void SetOperationDefinition(
        this RequestContext context,
        OperationDefinitionNode operationDefinition)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();

        if (operationInfo.Operation is not null)
        {
            throw new InvalidOperationException(
                "The operation definition cannot be set after "
                + "the operation was already compiled.");
        }

        operationInfo.Definition = operationDefinition;
    }

    public static bool TryGetOperation(
        this RequestContext context,
        [NotNullWhen(true)] out IOperation? operation)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operation = operationInfo.Operation;
        return operation is not null;
    }

    public static bool TryGetOperation(
        this RequestContext context,
        [NotNullWhen(true)] out IOperation? operation,
        [NotNullWhen(true)] out string? operationId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operation = operationInfo.Operation;
        operationId = operationInfo.Id;
        return operation is not null;
    }

    public static IOperation GetOperation(this RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.GetRequired<OperationInfo>().Operation
            ?? throw new InvalidOperationException("The operation is not initialized.");
    }

    public static void SetOperation(
        this RequestContext context,
        IOperation operation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operationInfo.Operation = operation;
        operationInfo.Id = operation.Id;
        operationInfo.Definition = operation.Definition;
    }

    public static bool TryGetOperationId(this RequestContext context, [NotNullWhen(true)] out string? operationId)
    {
        ArgumentNullException.ThrowIfNull(context);

        operationId = context.Features.GetOrSet<OperationInfo>().Id;
        return operationId is not null;
    }

    public static void SetOperationId(this RequestContext context, string operationId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var operationInfo = context.Features.GetOrSet<OperationInfo>();
        operationInfo.Id = operationId;
    }
}
