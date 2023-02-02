using System.Collections.Concurrent;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Batching;

internal static class ExportDirectiveHelper
{
    public const string Name = "export";
    public const string ExportedVariables = "HC.ExportedVariables";

    public static T AddExportedVariables<T>(
        this T builder,
        ConcurrentBag<ExportedVariable> exportedVariables)
        where T : IQueryRequestBuilder
    {
        builder.SetGlobalState(ExportedVariables, exportedVariables);
        return builder;
    }

    public static void ExportValueAsVariable(
        this IMiddlewareContext context,
        ExportDirective directive)
    {
        if (context.ContextData.TryGetValue(ExportedVariables, out var o)
            && o is ConcurrentBag<ExportedVariable> exp)
        {
            exp.Add(new ExportedVariable(
                directive.As ?? context.Selection.Field.Name,
                context.Selection.Type,
                context.Result));
        }
    }
}
