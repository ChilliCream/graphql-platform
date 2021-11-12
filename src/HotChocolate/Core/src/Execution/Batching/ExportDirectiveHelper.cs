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
        builder.SetProperty(
            ExportedVariables,
            exportedVariables);
        return builder;
    }

    public static void ExportValueAsVariable(
        this IDirectiveContext context)
    {
        if (context.ContextData.TryGetValue(ExportedVariables, out var o)
            && o is ConcurrentBag<ExportedVariable> exp)
        {
            exp.Add(new ExportedVariable(
                context.Directive.ToObject<ExportDirective>().As
                    ?? context.Selection.Field.Name.Value,
                context.Selection.Type,
                context.Result));
        }
    }
}
