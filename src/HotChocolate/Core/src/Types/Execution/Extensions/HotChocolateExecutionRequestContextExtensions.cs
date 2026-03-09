using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Execution;

public static class HotChocolateExecutionRequestContextExtensions
{
    extension(RequestContext context)
    {
        public bool TryGetOperationDefinition([NotNullWhen(true)] out OperationDefinitionNode? operationDefinition)
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

        public void SetOperationDefinition(OperationDefinitionNode operationDefinition)
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

        public bool TryGetOperation([NotNullWhen(true)] out Operation? operation)
        {
            ArgumentNullException.ThrowIfNull(context);

            var operationInfo = context.Features.GetOrSet<OperationInfo>();
            operation = operationInfo.Operation;
            return operation is not null;
        }

        public bool TryGetOperation([NotNullWhen(true)] out Operation? operation,
            [NotNullWhen(true)] out string? operationId)
        {
            ArgumentNullException.ThrowIfNull(context);

            var operationInfo = context.Features.GetOrSet<OperationInfo>();
            operation = operationInfo.Operation;
            operationId = operationInfo.Id;
            return operation is not null;
        }

        public Operation GetOperation()
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.Features.GetRequired<OperationInfo>().Operation
                ?? throw new InvalidOperationException("The operation is not initialized.");
        }

        public void SetOperation(Operation operation)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(operation);

            var operationInfo = context.Features.GetOrSet<OperationInfo>();
            operationInfo.Operation = operation;
            operationInfo.Id = operation.Id;
            operationInfo.Definition = operation.Definition;
        }

        public bool TryGetOperationId([NotNullWhen(true)] out string? operationId)
        {
            ArgumentNullException.ThrowIfNull(context);

            operationId = context.Features.GetOrSet<OperationInfo>().Id;
            return operationId is not null;
        }

        public void SetOperationId(string operationId)
        {
            ArgumentNullException.ThrowIfNull(context);

            var operationInfo = context.Features.GetOrSet<OperationInfo>();
            operationInfo.Id = operationId;
        }
    }
}
