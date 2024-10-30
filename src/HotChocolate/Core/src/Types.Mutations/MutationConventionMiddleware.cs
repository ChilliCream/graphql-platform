namespace HotChocolate.Types;

/// <summary>
/// This middleware ensures that the rewritten argument structure is remapped so that the
/// resolver can request the arguments in the original structure.
/// </summary>
internal sealed class MutationConventionMiddleware(
    FieldDelegate next,
    string inputArgumentName,
    IReadOnlyList<ResolverArgument> resolverArguments)
{
    private readonly FieldDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly string _inputArgumentName = inputArgumentName ??
        throw new ArgumentNullException(nameof(inputArgumentName));
    private readonly IReadOnlyList<ResolverArgument> _resolverArguments = resolverArguments ??
        throw new ArgumentNullException(nameof(resolverArguments));

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var input = context.ArgumentValue<IDictionary<string, object?>>(_inputArgumentName);
        var inputLiteral = context.ArgumentLiteral<ObjectValueNode>(_inputArgumentName)
            .Fields.ToDictionary(t => t.Name.Value, t => t.Value);

        var arguments = new Dictionary<string, ArgumentValue>(StringComparer.Ordinal);
        var preservedArguments = context.ReplaceArguments(arguments);

        foreach (var argument in _resolverArguments)
        {
            input.TryGetValue(argument.Name, out var value);

            var omitted = false;
            if (!inputLiteral.TryGetValue(argument.Name, out var valueLiteral))
            {
                omitted = true;
                valueLiteral = argument.DefaultValue;
                value = null;
            }
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
                    !omitted,
                    omitted,
                    value,
                    valueLiteral));
        }

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.ReplaceArguments(preservedArguments);
        }
    }
}
