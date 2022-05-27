using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.ApolloFederation.Helpers;

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
        Task<object?>[] tasks = ArrayPool<Task<object?>>.Shared.Rent(representations.Count);
        var result = new object?[representations.Count];

        try
        {
            for (var i = 0; i < representations.Count; i++)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                Representation current = representations[i];

                if (schema.TryGetType<ObjectType>(current.TypeName, out ObjectType? objectType) &&
                    objectType.ContextData.TryGetValue(EntityResolver, out var value) &&
                    value is FieldResolverDelegate resolver)
                {
                    context.SetLocalState(TypeField, objectType);
                    context.SetLocalState(DataField, current.Data);

                    tasks[i] = resolver.Invoke(new ResolverContextProxy(context)).AsTask();
                }
                else
                {
                    throw ThrowHelper.EntityResolver_NoResolverFound();
                }
            }

            for (var i = 0; i < representations.Count; i++)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                Task<object?> task = tasks[i];
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
        }
        finally
        {
            ArrayPool<Task<object?>>.Shared.Return(tasks, true);
        }

        return result;
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
    {
        Path itemPath = PathFactory.Instance.Append(context.Path, item);
        context.ReportError(ex, error => error.SetPath(itemPath));
    }
}
