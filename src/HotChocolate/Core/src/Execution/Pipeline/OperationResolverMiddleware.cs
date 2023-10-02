using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.WellKnownDirectives;
using static HotChocolate.Execution.Pipeline.PipelineTools;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObjectPool<OperationCompiler> _operationCompilerPool;
    private readonly VariableCoercionHelper _coercionHelper;
    private readonly IReadOnlyList<IOperationCompilerOptimizer>? _optimizers;

    public OperationResolverMiddleware(
        RequestDelegate next,
        ObjectPool<OperationCompiler> operationCompilerPool,
        IEnumerable<IOperationCompilerOptimizer> optimizers,
        VariableCoercionHelper coercionHelper)
    {
        if (optimizers is null)
        {
            throw new ArgumentNullException(nameof(optimizers));
        }

        _next = next ?? 
            throw new ArgumentNullException(nameof(next));
        _operationCompilerPool = operationCompilerPool ?? 
            throw new ArgumentNullException(nameof(operationCompilerPool));
        _coercionHelper = coercionHelper ?? 
            throw new ArgumentNullException(nameof(coercionHelper));
        _optimizers = optimizers.ToArray();
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Operation is not null)
        {
            await _next(context).ConfigureAwait(false);
        }
        else if (context.Document is not null && context.IsValidDocument)
        {
            using (context.DiagnosticEvents.CompileOperation(context))
            {
                var operationDef = context.Document.GetOperation(context.Request.OperationName);
                var operationType = ResolveOperationType(operationDef.Operation, context.Schema);

                if (operationType is null)
                {
                    context.Result = RootTypeNotFound(operationDef.Operation);
                    return;
                }

                context.Operation = CompileOperation(
                    context,
                    context.OperationId ?? Guid.NewGuid().ToString("N"),
                    operationDef,
                    operationType);
                context.OperationId = context.Operation.Id;
            }

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = StateInvalidForOperationResolver();
        }
    }

    private IOperation CompileOperation(
        IRequestContext context,
        string operationId,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType)
    {
        var compiler = _operationCompilerPool.Get();
        var operation = compiler.Compile(
            operationId,
            operationDefinition,
            operationType,
            context.Document!,
            context.Schema,
            _optimizers,
            IsNullBubblingEnabled(context, operationDefinition));
        _operationCompilerPool.Return(compiler);
        return operation;
    }

    private static ObjectType? ResolveOperationType(
        OperationType operationType,
        ISchema schema)
        => operationType switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw ThrowHelper.RootTypeNotSupported(operationType)
        };

    private bool IsNullBubblingEnabled(IRequestContext context, OperationDefinitionNode operationDefinition)
    {
        if (!context.Schema.ContextData.ContainsKey(EnableTrueNullability) ||
            operationDefinition.Directives.Count == 0)
        {
            return true;
        }
        
        var enabled = true;

        for (var i = 0; i < operationDefinition.Directives.Count; i++)
        {
            var directive = operationDefinition.Directives[i];

            if (!directive.Name.Value.EqualsOrdinal(NullBubbling))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var argument = directive.Arguments[j];

                if (argument.Name.Value.EqualsOrdinal(Enable))
                {
                    if (argument.Value is BooleanValueNode b)
                    {
                        enabled = b.Value;
                        break;
                    }
                        
                    if (argument.Value is VariableNode v)
                    {
                        enabled = CoerceVariable(context, operationDefinition, v);
                        break;
                    }

                    throw new GraphQLException(NoNullBubbling_ArgumentValue_NotAllowed(argument));
                }
            }

            break;
        }

        return enabled;
    }

    private bool CoerceVariable(
        IRequestContext context, 
        OperationDefinitionNode operationDefinition, 
        VariableNode variable)
    {
        var variables = CoerceVariables(context, _coercionHelper, operationDefinition.VariableDefinitions);
        return variables.GetVariable<bool>(variable.Name.Value);
    } 
}