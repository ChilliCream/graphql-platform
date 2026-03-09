using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Provides extension methods for inserting, prepending, and combining middleware configurations in pipeline modifier lists.
/// </summary>
public static class MiddlewareConfigurationExtensions
{
    /// <summary>
    /// Appends a dispatch middleware configuration to the pipeline, optionally after a specific middleware identified by key.
    /// </summary>
    /// <param name="configurations">The list of pipeline modifiers.</param>
    /// <param name="configuration">The middleware configuration to append.</param>
    /// <param name="after">The key of the middleware to insert after, or <c>null</c> to append at the end.</param>
    public static void Append(
        this List<Action<List<DispatchMiddlewareConfiguration>>> configurations,
        DispatchMiddlewareConfiguration configuration,
        string? after)
    {
        if (after is null)
        {
            configurations.Add(pipeline => pipeline.Add(configuration));
            return;
        }

        configurations.Add(pipeline =>
        {
            var index = pipeline.FindIndex(m => m.Key == after);
            if (index == -1)
            {
                throw new InvalidOperationException($"Middleware with key {after} not found");
            }

            pipeline.Insert(index + 1, configuration);
        });
    }

    /// <summary>
    /// Prepends a dispatch middleware configuration to the pipeline, optionally before a specific middleware identified by key.
    /// </summary>
    /// <param name="configurations">The list of pipeline modifiers.</param>
    /// <param name="configuration">The middleware configuration to prepend.</param>
    /// <param name="before">The key of the middleware to insert before, or <c>null</c> to prepend at the beginning.</param>
    public static void Prepend(
        this List<Action<List<DispatchMiddlewareConfiguration>>> configurations,
        DispatchMiddlewareConfiguration configuration,
        string? before)
    {
        if (before is null)
        {
            configurations.Add(pipeline => pipeline.Insert(0, configuration));
            return;
        }

        configurations.Add(pipeline =>
        {
            var index = pipeline.FindIndex(m => m.Key == before);
            if (index == -1)
            {
                throw new InvalidOperationException($"Middleware with key {before} not found");
            }

            pipeline.Insert(index, configuration);
        });
    }

    /// <summary>
    /// Combines the base dispatch middlewares with additional configurations and pipeline modifiers.
    /// </summary>
    /// <param name="middlewares">The base dispatch middleware pipeline.</param>
    /// <param name="configurations">Additional middleware configurations to prepend.</param>
    /// <param name="modifiers">Pipeline modifiers to apply after combining.</param>
    /// <returns>The combined dispatch middleware pipeline.</returns>
    public static ImmutableArray<DispatchMiddlewareConfiguration> Combine(
        this ImmutableArray<DispatchMiddlewareConfiguration> middlewares,
        IReadOnlyList<DispatchMiddlewareConfiguration> configurations,
        IReadOnlyList<Action<List<DispatchMiddlewareConfiguration>>> modifiers)
    {
        if (configurations.Count > 0)
        {
            middlewares = [.. configurations, .. middlewares];
        }

        if (modifiers.Count > 0)
        {
            var dispatchList = middlewares.ToList();
            foreach (var modifier in modifiers)
            {
                modifier(dispatchList);
            }
        }

        return middlewares.ToImmutableArray();
    }

    extension(List<Action<List<ReceiveMiddlewareConfiguration>>> configurations)
    {
        /// <summary>
        /// Appends a receive middleware configuration after the middleware with the specified key, or at the end of the pipeline.
        /// </summary>
        /// <param name="configuration">The middleware configuration to add.</param>
        /// <param name="after">The key of the middleware to insert after, or <c>null</c> to append at the end.</param>
        public void Append(ReceiveMiddlewareConfiguration configuration, string? after)
        {
            if (after is null)
            {
                configurations.Add(pipeline => pipeline.Add(configuration));
                return;
            }

            configurations.Add(pipeline =>
            {
                var index = pipeline.FindIndex(m => m.Key == after);
                if (index == -1)
                {
                    throw new InvalidOperationException($"Middleware with key {after} not found");
                }

                pipeline.Insert(index + 1, configuration);
            });
        }

        /// <summary>
        /// Prepends a receive middleware configuration before the middleware with the specified key, or at the beginning of the pipeline.
        /// </summary>
        /// <param name="configuration">The middleware configuration to add.</param>
        /// <param name="before">The key of the middleware to insert before, or <c>null</c> to prepend at the beginning.</param>
        public void Prepend(ReceiveMiddlewareConfiguration configuration, string? before)
        {
            if (before is null)
            {
                configurations.Add(pipeline => pipeline.Insert(0, configuration));
                return;
            }

            configurations.Add(pipeline =>
            {
                var index = pipeline.FindIndex(m => m.Key == before);
                if (index == -1)
                {
                    throw new InvalidOperationException($"Middleware with key {before} not found");
                }

                pipeline.Insert(index, configuration);
            });
        }
    }

    /// <summary>
    /// Combines the base receive middlewares with additional configurations and pipeline modifiers.
    /// </summary>
    /// <param name="middlewares">The base receive middleware pipeline.</param>
    /// <param name="configurations">Additional middleware configurations to prepend.</param>
    /// <param name="modifiers">Pipeline modifiers to apply after combining.</param>
    /// <returns>The combined receive middleware pipeline.</returns>
    public static ImmutableArray<ReceiveMiddlewareConfiguration> Combine(
        this ImmutableArray<ReceiveMiddlewareConfiguration> middlewares,
        IReadOnlyList<ReceiveMiddlewareConfiguration> configurations,
        IReadOnlyList<Action<List<ReceiveMiddlewareConfiguration>>> modifiers)
    {
        if (configurations.Count > 0)
        {
            middlewares = [.. configurations, .. middlewares];
        }

        if (modifiers.Count > 0)
        {
            var receiveList = middlewares.ToList();
            foreach (var modifier in modifiers)
            {
                modifier(receiveList);
            }

            middlewares = [.. receiveList];
        }

        return middlewares.ToImmutableArray();
    }

    extension(List<Action<List<ConsumerMiddlewareConfiguration>>> configurations)
    {
        /// <summary>
        /// Appends a consumer middleware configuration after the middleware with the specified key, or at the end of the pipeline.
        /// </summary>
        /// <param name="configuration">The middleware configuration to add.</param>
        /// <param name="after">The key of the middleware to insert after, or <c>null</c> to append at the end.</param>
        public void Append(ConsumerMiddlewareConfiguration configuration, string? after)
        {
            if (after is null)
            {
                configurations.Add(pipeline => pipeline.Add(configuration));
                return;
            }

            configurations.Add(pipeline =>
            {
                var index = pipeline.FindIndex(m => m.Key == after);
                if (index == -1)
                {
                    throw new InvalidOperationException($"Middleware with key {after} not found");
                }

                pipeline.Insert(index + 1, configuration);
            });
        }

        /// <summary>
        /// Prepends a consumer middleware configuration before the middleware with the specified key, or at the beginning of the pipeline.
        /// </summary>
        /// <param name="configuration">The middleware configuration to add.</param>
        /// <param name="before">The key of the middleware to insert before, or <c>null</c> to prepend at the beginning.</param>
        public void Prepend(ConsumerMiddlewareConfiguration configuration, string? before)
        {
            if (before is null)
            {
                configurations.Add(pipeline => pipeline.Insert(0, configuration));
                return;
            }

            configurations.Add(pipeline =>
            {
                var index = pipeline.FindIndex(m => m.Key == before);
                if (index == -1)
                {
                    throw new InvalidOperationException($"Middleware with key {before} not found");
                }

                pipeline.Insert(index, configuration);
            });
        }
    }

    /// <summary>
    /// Combines the base consumer middlewares with additional configurations and pipeline modifiers.
    /// </summary>
    /// <param name="middlewares">The base consumer middleware pipeline.</param>
    /// <param name="configurations">Additional middleware configurations to prepend.</param>
    /// <param name="modifiers">Pipeline modifiers to apply after combining.</param>
    /// <returns>The combined consumer middleware pipeline.</returns>
    public static ImmutableArray<ConsumerMiddlewareConfiguration> Combine(
        this ImmutableArray<ConsumerMiddlewareConfiguration> middlewares,
        IReadOnlyList<ConsumerMiddlewareConfiguration> configurations,
        IReadOnlyList<Action<List<ConsumerMiddlewareConfiguration>>> modifiers)
    {
        if (configurations.Count > 0)
        {
            middlewares = [.. configurations, .. middlewares];
        }

        if (modifiers.Count > 0)
        {
            var handlerList = middlewares.ToList();
            foreach (var modifier in modifiers)
            {
                modifier(handlerList);
            }

            middlewares = [.. handlerList];
        }

        return middlewares.ToImmutableArray();
    }
}
