using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public static class SymbolExtensions
{
    public static bool IsParent(this IParameterSymbol parameter)
        => parameter.IsThis ||
            parameter
                .GetAttributes()
                .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.ParentAttribute);

    public static bool IsCancellationToken(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.CancellationToken;

    public static bool IsClaimsPrincipal(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.ClaimsPrincipal;

    public static bool IsDocumentNode(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.DocumentNode;

    public static bool IsEventMessage(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownAttributes.EnumTypeAttribute;

    public static ResolverResultKind GetResultKind(this IMethodSymbol method)
    {
        const string task = $"{WellKnownTypes.Task}<";
        const string valueTask = $"{WellKnownTypes.ValueTask}<";
        const string taskEnumerable = $"{WellKnownTypes.Task}<{WellKnownTypes.AsyncEnumerable}<";
        const string valueTaskEnumerable = $"{WellKnownTypes.ValueTask}<{WellKnownTypes.AsyncEnumerable}<";

        if (method.ReturnsVoid || method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            return ResolverResultKind.Invalid;
        }

        var returnType = method.ReturnType.ToDisplayString();

        if (returnType.Equals(WellKnownTypes.Task) ||
            returnType.Equals(WellKnownTypes.ValueTask))
        {
            return ResolverResultKind.Invalid;
        }

        if (returnType.StartsWith(task) ||
            returnType.StartsWith(valueTask))
        {
            if (returnType.StartsWith(taskEnumerable) ||
                returnType.StartsWith(valueTaskEnumerable))
            {
                return ResolverResultKind.TaskAsyncEnumerable;
            }

            return ResolverResultKind.Task;
        }

        if (returnType.StartsWith(WellKnownTypes.Executable))
        {
            return ResolverResultKind.Executable;
        }

        if (returnType.StartsWith(WellKnownTypes.Queryable))
        {
            return ResolverResultKind.Queryable;
        }

        if (returnType.StartsWith(WellKnownTypes.AsyncEnumerable))
        {
            return ResolverResultKind.AsyncEnumerable;
        }

        return ResolverResultKind.Pure;
    }
}
