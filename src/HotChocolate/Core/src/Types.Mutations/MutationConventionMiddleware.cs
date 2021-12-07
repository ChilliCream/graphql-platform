using System.Linq;
using HotChocolate.Execution.Processing;

#nullable enable

namespace HotChocolate.Types;

internal sealed class MutationConventionMiddleware
{
    private readonly FieldDelegate _next;
    private readonly string _inputArgumentName;
    private readonly IReadOnlyList<ResolverArgument> _resolverArguments;

    public MutationConventionMiddleware(
        FieldDelegate next,
        string inputArgumentName,
        IReadOnlyList<ResolverArgument> resolverArguments)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _inputArgumentName = inputArgumentName ??
            throw new ArgumentNullException(nameof(inputArgumentName));
        _resolverArguments = resolverArguments ??
            throw new ArgumentNullException(nameof(resolverArguments));
    }

    public ValueTask InvokeAsync(IMiddlewareContext context)
        => InvokeInternalAsync(((MiddlewareContext)context));

    private async ValueTask InvokeInternalAsync(MiddlewareContext context)
    {
        var input = context.ArgumentValue<IDictionary<string, object?>>(_inputArgumentName);
        var inputLiteral = context.ArgumentLiteral<ObjectValueNode>(_inputArgumentName)
            .Fields.ToDictionary(t => t.Name.Value, t => t.Value);
        var inputArgument = context.Arguments[_inputArgumentName];

        var preservedArguments = context.Arguments;
        var arguments = new Dictionary<NameString, ArgumentValue>();

        foreach (ResolverArgument argument in _resolverArguments)
        {
            input.TryGetValue(argument.Name, out var value);

            inputLiteral.TryGetValue(argument.Name, out var valueLiteral);
            valueLiteral ??= NullValueNode.Default;

            if (!valueLiteral.TryGetValueKind(out ValueKind kind))
            {
                kind = ValueKind.Unknown;
            }

            arguments.Add(
                argument.Name,
                new ArgumentValue(
                    argument,
                    kind,
                    inputArgument.IsFinal,
                    inputArgument.IsImplicit,
                    value,
                    valueLiteral));
        }

        try
        {
            context.Arguments = arguments;
            await _next(context);
        }
        finally
        {
            context.Arguments = preservedArguments;
        }
    }
}

