using System.Linq;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This middleware ensures that the rewritten argument structure is remapped so that the
/// resolver can request the arguments in the original structure.
/// </summary>
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

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var input = context.ArgumentValue<IDictionary<string, object?>>(_inputArgumentName);
        var inputLiteral = context.ArgumentLiteral<ObjectValueNode>(_inputArgumentName)
            .Fields.ToDictionary(t => t.Name.Value, t => t.Value);

        var arguments = new Dictionary<string, ArgumentValue>(StringComparer.Ordinal);
        var preservedArguments = context.ReplaceArguments(arguments);
        var inputArgument = preservedArguments[_inputArgumentName];

        foreach (var argument in _resolverArguments)
        {
            input.TryGetValue(argument.Name, out var value);

            inputLiteral.TryGetValue(argument.Name, out var valueLiteral);
            valueLiteral ??= NullValueNode.Default;

            if (!valueLiteral.TryGetValueKind(out var kind))
            {
                kind = ValueKind.Unknown;
            }

            arguments.Add(
                argument.Name,
                new ArgumentValue(
                    argument,
                    kind,
                    true,
                    inputArgument.IsDefaultValue,
                    value,
                    valueLiteral));
        }

        try
        {
            await _next(context);

            context.Result ??= Null;
        }
        finally
        {
            context.ReplaceArguments(preservedArguments);
        }
    }

    internal static object Null { get; } = new();
}

