using System;

namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="IQueryRequestBuilder"/>.
/// </summary>
public static class QueryRequestBuilderExtensions
{
    /// <summary>
    /// Allows introspection usage in the current request.
    /// </summary>
    public static IQueryRequestBuilder AllowIntrospection(
        this IQueryRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.IntrospectionAllowed, null);

    /// <summary>
    /// Sets the error message for when the introspection is not allowed.
    /// </summary>
    public static IQueryRequestBuilder SetIntrospectionNotAllowedMessage(
        this IQueryRequestBuilder builder,
        string message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return builder.SetGlobalState(WellKnownContextData.IntrospectionMessage, message);
    }

    /// <summary>
    /// Sets the error message for when the introspection is not allowed.
    /// </summary>
    public static IQueryRequestBuilder SetIntrospectionNotAllowedMessage(
        this IQueryRequestBuilder builder,
        Func<string> messageFactory)
    {
        if (messageFactory is null)
        {
            throw new ArgumentNullException(nameof(messageFactory));
        }

        return builder.SetGlobalState(WellKnownContextData.IntrospectionMessage, messageFactory);
    }

    /// <summary>
    /// Skips the operation complexity analysis of this request.
    /// </summary>
    public static IQueryRequestBuilder SkipComplexityAnalysis(
        this IQueryRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.SkipComplexityAnalysis, null);

    /// <summary>
    /// Set allowed complexity for this request and override the global allowed complexity.
    /// </summary>
    public static IQueryRequestBuilder SetMaximumAllowedComplexity(
        this IQueryRequestBuilder builder,
        int maximumAllowedComplexity) =>
        builder.SetGlobalState(
            WellKnownContextData.MaximumAllowedComplexity,
            maximumAllowedComplexity);

    /// <summary>
    /// Marks the current request to allow non-persisted queries.
    /// </summary>
    public static IQueryRequestBuilder AllowNonPersistedQuery(
        this IQueryRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.NonPersistedQueryAllowed, true);

    /// <summary>
    /// Skips the request execution depth analysis.
    /// </summary>
    public static IQueryRequestBuilder SkipExecutionDepthAnalysis(
        this IQueryRequestBuilder builder) =>
        builder.SetGlobalState(WellKnownContextData.SkipDepthAnalysis, null);

    /// <summary>
    /// Set allowed execution depth for this request and override the
    /// global allowed execution depth.
    /// </summary>
    public static IQueryRequestBuilder SetMaximumAllowedExecutionDepth(
        this IQueryRequestBuilder builder,
        int maximumAllowedDepth) =>
        builder.SetGlobalState(WellKnownContextData.MaxAllowedExecutionDepth, maximumAllowedDepth);

    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="IQueryResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IQueryResultBuilder"/>.
    /// </param>
    /// <param name="clean">
    /// A cleanup task that will be executed when this result is disposed.
    /// </param>
    public static void RegisterForCleanup(this IQueryResultBuilder builder, Action clean)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (clean is null)
        {
            throw new ArgumentNullException(nameof(clean));
        }

        builder.RegisterForCleanup(() =>
        {
            clean();
            return default;
        });
    }

    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="IQueryResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IQueryResultBuilder"/>.
    /// </param>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public static void RegisterForCleanup(this IQueryResultBuilder builder, IDisposable disposable)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (disposable is null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        builder.RegisterForCleanup(disposable.Dispose);
    }

    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="IQueryResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IExecutionResult"/>.
    /// </param>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public static void RegisterForCleanup(
        this IQueryResultBuilder builder,
        IAsyncDisposable disposable)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (disposable is null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        builder.RegisterForCleanup(disposable.DisposeAsync);
    }
}
