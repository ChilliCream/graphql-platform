using System.Buffers;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class contains the _entities resolver method.
/// </summary>
internal static class EntitiesResolver
{
    public static async Task<IReadOnlyList<object?>> ResolveAsync(
        ISchema schema,
        IReadOnlyList<Representation> representations,
        IResolverContext context)
    {
        var tasks = ArrayPool<Task<object?>>.Shared.Rent(representations.Count);
        var result = new object?[representations.Count];

        for (var i = 0; i < representations.Count; i++)
        {
            context.RequestAborted.ThrowIfCancellationRequested();

            var current = representations[i];

            if (schema.TryGetType<ObjectType>(current.TypeName, out var objectType) &&
                objectType.ContextData.TryGetValue(EntityResolver, out var value) &&
                value is FieldResolverDelegate resolver)
            {
                // We clone the resolver context here so that we can split the work
                // into sub tasks that can be awaited in parallel and produce separate results.
                var entityContext = context.Clone();

                entityContext.SetLocalState(TypeField, objectType);
                entityContext.SetLocalState(DataField, current.Data);

                tasks[i] = resolver.Invoke(entityContext).AsTask();
            }
            else
            {
                throw ThrowHelper.EntityResolver_NoResolverFound();
            }
        }

        for (var i = 0; i < representations.Count; i++)
        {
            context.RequestAborted.ThrowIfCancellationRequested();

            var task = tasks[i];
            if (task.IsCompleted)
            {
                if (task.Exception is null)
                {
                    result[i] = task.Result;
                }
                else
                {
                    result[i] = null;
                    ReportError(context, i, task.Exception);
                }
            }
            else
            {
                try
                {
                    result[i] = await task;
                }
                catch (Exception ex)
                {
                    result[i] = null;
                    ReportError(context, i, ex);
                }
            }
        }

        ArrayPool<Task<object?>>.Shared.Return(tasks, true);
        return result;
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
    {
        var itemPath = context.Path.Append(item);
        context.ReportError(ex, error => error.SetPath(itemPath));
    }
}
