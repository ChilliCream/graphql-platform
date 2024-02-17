using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware(
    RequestDelegate next,
    VariableCoercionHelper coercionHelper)
{
    private readonly RequestDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly VariableCoercionHelper _coercionHelper = coercionHelper ??
        throw new ArgumentNullException(nameof(coercionHelper));

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Operation is not null)
        {
            CoerceVariables(
                context,
                _coercionHelper,
                context.Operation.Definition.VariableDefinitions);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationVariableCoercion();
        }
    }
    
    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var coercionHelper = core.Services.GetRequiredService<VariableCoercionHelper>();
            var middleware = new OperationVariableCoercionMiddleware(next, coercionHelper);
            return context => middleware.InvokeAsync(context);
        };
}
