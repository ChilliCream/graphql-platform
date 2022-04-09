using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.ApolloFederation.Helpers;

/// <summary>
/// This class contains the _entities resolver method.
/// </summary>
internal static class EntitiesResolver
{
    public static async Task<List<object?>> ResolveAsync(
        ISchema schema,
        IReadOnlyList<Representation> representations,
        IResolverContext context)
    {
        Task<object?>[] tasks = ArrayPool<Task<object?>>.Shared.Rent(representations.Count);
        tasks.AsSpan().Slice(0, representations.Count).Clear();
        var result = new object?[representations.Count];

        try
        {
            foreach (var indexedRepresentation in representations.Select((representation, index) => (representation, index)))
            {
                if (schema.TryGetType<ObjectType>(indexedRepresentation.representation.TypeName, out var objectType) &&
                    objectType.ContextData.TryGetValue(EntityResolver, out var value) &&
                    value is FieldResolverDelegate resolver)
                {
                    context.SetLocalState(TypeField, objectType);
                    context.SetLocalState(DataField, indexedRepresentation.representation.Data);

                    tasks[indexedRepresentation.index] = resolver.Invoke(context).AsTask();
                }
                else
                {
                    throw ThrowHelper.EntityResolver_NoResolverFound();
                }
            }

            foreach (var indexedRepresentation in representations.Select((representation, index) => (representation, index)))
            {
                Task<object?> task = tasks[indexedRepresentation.index];
                if (task.IsCompleted)
                {
                    if (task.Exception is null)
                    {
                        result[indexedRepresentation.index] = task.Result;
                    }
                    else
                    {
                        result[indexedRepresentation.index] = null;
                        ReportError(context, indexedRepresentation.index, task.Exception);
                    }
                }
                else
                {
                    try
                    {
                        result[indexedRepresentation.index] = await task;
                    }
                    catch (Exception ex)
                    {
                        result[indexedRepresentation.index] = null;
                        ReportError(context, indexedRepresentation.index, ex);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<Task<object?>>.Shared.Return(tasks);
        }

        return result.ToList();
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
    {
        Path itemPath = context.Path.Append(item);
        context.ReportError(ex, error => error.SetPath(itemPath));
    }
}
