using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.Stitching.Processing.DelegationHelpers;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Processing;

internal sealed class QueryResultMiddleware
{
    private readonly FieldDelegate _next;

    public QueryResultMiddleware(FieldDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        if (context.Result is IQueryResult result &&
            context.ScopedContextData.TryGetValue(SchemaName, out var targetSchemaValue) &&
            context.ScopedContextData.TryGetValue(WellKnownContextData.Path, out var pathValue) &&
            context.ScopedContextData.TryGetValue(ReversePath, out var reversePathValue) &&
            targetSchemaValue is NameString targetSchema &&
            pathValue is IImmutableStack<SelectionPathComponent> path &&
            reversePathValue is IImmutableStack<SelectionPathComponent> reversePath)
        {
            context.RegisterForCleanup(result.Dispose);
            CopyResultContextData(context, result);

            var value = ExtractData(result.Data, reversePath, context.ResponseName);
            context.Result = value is null or NullValueNode ? null : new SerializedData(value);
            if (result.Errors is not null)
            {
                ReportErrors(targetSchema, context, path, result.Errors);
            }
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }

    private static void CopyResultContextData(IResolverContext context, IExecutionResult result)
    {
        if (result.ContextData?.Count > 0)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object?>();
            builder.AddRange(context.ScopedContextData);
            builder.AddRange(result.ContextData);
            context.ScopedContextData = builder.ToImmutableDictionary();
        }
    }
}
