using System.Buffers;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.FederationContextData;
using static HotChocolate.ApolloFederation.WellKnownArgumentNames;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class contains the _entities resolver method.
/// </summary>
internal static class EntitiesResolver
{
#if NET9_0_OR_GREATER
    private static readonly Lock _representationCounterLock = new();
#else
    private static readonly object _representationCounterLock = new();
#endif

    public static Task<IReadOnlyList<object?>> ResolveAsync(
        IResolverContext context,
        int maxAllowedRepresentations)
    {
        var count = context.ArgumentKind(Representations) is ValueKind.List
            ? context.ArgumentLiteral<ListValueNode>(Representations).Items.Count
            : 1;

        if (!GetRepresentationCounter(context).TryAdd(count, maxAllowedRepresentations))
        {
            throw ThrowHelper.EntityResolver_TooManyRepresentations(maxAllowedRepresentations);
        }

        return ResolveAsync(
            context.Schema,
            context.ArgumentValue<IReadOnlyList<Representation>>(Representations),
            context);
    }

    public static async Task<IReadOnlyList<object?>> ResolveAsync(
        ISchema schema,
        IReadOnlyList<Representation> representations,
        IResolverContext context)
    {
        var tasks = ArrayPool<Task<object?>>.Shared.Rent(representations.Count);
        var result = new object?[representations.Count];
        var started = 0;

        try
        {
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
                    started++;
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

            return result;
        }
        finally
        {
            // wait for all started entity resolvers before completing the field.
            for (var i = 0; i < started; i++)
            {
#if NET8_0_OR_GREATER
                await ((Task)tasks[i]).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
                try
                {
                    await tasks[i].ConfigureAwait(false);
                }
                catch
                {
                    // the outcome of the task is not used here.
                }
#endif
            }

            ArrayPool<Task<object?>>.Shared.Return(tasks, true);
        }
    }

    private static RepresentationCounter GetRepresentationCounter(IResolverContext context)
    {
        var contextData = context.ContextData;

        if (contextData.TryGetValue(RepresentationCount, out var value))
        {
            return (RepresentationCounter)value!;
        }

        lock (_representationCounterLock)
        {
            if (!contextData.TryGetValue(RepresentationCount, out value))
            {
                value = new RepresentationCounter();
                contextData[RepresentationCount] = value;
            }

            return (RepresentationCounter)value!;
        }
    }

    private static void ReportError(IResolverContext context, int item, Exception ex)
    {
        var itemPath = context.Path.Append(item);
        context.ReportError(ex, error => error.SetPath(itemPath));
    }

    private sealed class RepresentationCounter
    {
        private int _value;

        public bool TryAdd(int count, int maxAllowedRepresentations)
        {
            var current = Volatile.Read(ref _value);

            while (count <= maxAllowedRepresentations - current)
            {
                var previous = Interlocked.CompareExchange(ref _value, current + count, current);

                if (previous == current)
                {
                    return true;
                }

                current = previous;
            }

            return false;
        }
    }
}
