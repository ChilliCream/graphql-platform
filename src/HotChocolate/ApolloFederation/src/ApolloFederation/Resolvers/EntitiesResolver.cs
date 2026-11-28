using System.Buffers;
using System.Collections.Immutable;
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
    private static readonly Lock s_representationCounterLock = new();
#else
    private static readonly object s_representationCounterLock = new();
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
        Schema schema,
        IReadOnlyList<Representation> representations,
        IResolverContext context)
    {
        var tasks = ArrayPool<Task<object?>?>.Shared.Rent(representations.Count);
        var result = new object?[representations.Count];
        var started = 0;
        var state = default(EntityState);

        try
        {
            for (var i = 0; i < representations.Count; i++)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                var current = representations[i];

                if (schema.Types.TryGetType<ObjectType>(current.TypeName, out var objectType)
                    && objectType.Features.TryGet(out ReferenceResolver? entity))
                {
                    // We clone the resolver context here so that we can split the work
                    // into subtasks that can be awaited in parallel and produce separate results.
                    var entityContext = context.Clone();

                    SetEntityState(entityContext, objectType, current.Data, ref state);

                    Track(
                        ResolveEntityAsync(entity.Resolver, entityContext, objectType, current.Data),
                        out tasks[i],
                        ref result[i]);
                    started++;
                    continue;
                }

                if (schema.Types.TryGetType<InterfaceType>(current.TypeName, out var interfaceType)
                    && interfaceType.Features.TryGet(out ReferenceResolver? entityInterface))
                {
                    // We clone the resolver context here so that we can split the work
                    // into subtasks that can be awaited in parallel and produce separate results.
                    var entityContext = context.Clone();

                    SetEntityState(entityContext, interfaceType, current.Data, ref state);

                    Track(entityInterface.Resolver.Invoke(entityContext), out tasks[i], ref result[i]);
                    started++;
                    continue;
                }

                throw ThrowHelper.EntityResolver_NoResolverFound();
            }

            for (var i = 0; i < representations.Count; i++)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                var task = tasks[i];

                // the entity of a resolver that completed while it was started is already in the result.
                if (task is null)
                {
                    continue;
                }

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
                if (tasks[i] is Task task)
                {
                    await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }

            ArrayPool<Task<object?>?>.Shared.Return(tasks, true);
        }
    }

    /// <summary>
    /// Sets the type and the representation of an entity as local state of its resolver context.
    /// </summary>
    private static void SetEntityState(
        IResolverContext entityContext,
        ITypeDefinition type,
        IValueNode representation,
        ref EntityState state)
    {
        var localState = entityContext.LocalContextData;

        if (!ReferenceEquals(state.Type, type) || !ReferenceEquals(state.Source, localState))
        {
            state.Type = type;
            state.Source = localState;
            state.SourceWithType = localState.SetItem(TypeField, type);
        }

        entityContext.LocalContextData = state.SourceWithType!.SetItem(DataField, representation);
    }

    /// <summary>
    /// Stores the entity of a resolver that has completed successfully in <paramref name="entity"/>,
    /// otherwise stores the task of the resolver in <paramref name="task"/>.
    /// </summary>
    private static void Track(ValueTask<object?> entityTask, out Task<object?>? task, ref object? entity)
    {
        if (entityTask.IsCompletedSuccessfully)
        {
            entity = entityTask.Result;
            task = null;
            return;
        }

        task = entityTask.AsTask();
    }

    private static async ValueTask<object?> ResolveEntityAsync(
        FieldResolverDelegate resolver,
        IResolverContext context,
        ObjectType type,
        IValueNode representation)
    {
        var entity = await resolver(context);

        if (entity is not null
            && type.Features.TryGet(out ExternalSetter? externalSetter))
        {
            externalSetter.Invoke(context.Schema, type, representation, entity);
        }

        return entity;
    }

    private static RepresentationCounter GetRepresentationCounter(IResolverContext context)
    {
        var contextData = context.ContextData;

        if (contextData.TryGetValue(RepresentationCount, out var value))
        {
            return (RepresentationCounter)value!;
        }

        lock (s_representationCounterLock)
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

    /// <summary>
    /// The local state of the last entity with its type, reused while the type and the source state repeat.
    /// </summary>
    private struct EntityState
    {
        public ITypeDefinition? Type;
        public IImmutableDictionary<string, object?>? Source;
        public IImmutableDictionary<string, object?>? SourceWithType;
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
