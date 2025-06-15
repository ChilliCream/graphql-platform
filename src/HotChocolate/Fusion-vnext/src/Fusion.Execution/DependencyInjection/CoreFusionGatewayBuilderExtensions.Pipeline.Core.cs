using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(
                    new RequestMiddlewareConfiguration((_, n) => middleware(n), key))));
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddleware middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(
                    new RequestMiddlewareConfiguration(middleware, key))));
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddlewareConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return Configure(
            builder,
            options => options.PipelineModifiers.Add(
                pipeline => pipeline.Add(configuration)));
    }

    public static IFusionGatewayBuilder AppendUseRequest(
        this IFusionGatewayBuilder builder,
        string after,
        RequestMiddleware middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(after);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration(middleware, key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    var index = GetIndex(pipeline, after);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{after}` was not found.");
                    }

                    pipeline.Insert(index + 1, configuration);
                });
            });
    }

    public static IFusionGatewayBuilder AppendUseRequest(
        this IFusionGatewayBuilder builder,
        string after,
        RequestMiddlewareConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(after);
        ArgumentNullException.ThrowIfNull(configuration);

        return Configure(
            builder,
            options =>
            {
                options.PipelineModifiers.Add(pipeline =>
                {
                    var index = GetIndex(pipeline, after);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{after}` was not found.");
                    }

                    pipeline.Insert(index + 1, configuration);
                });
            });
    }

    public static IFusionGatewayBuilder InsertUseRequest(
        this IFusionGatewayBuilder builder,
        string before,
        RequestMiddleware middleware,
        string? key = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(before);
        ArgumentNullException.ThrowIfNull(middleware);

        return Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration(middleware, key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    var index = GetIndex(pipeline, before);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{before}` was not found.");
                    }

                    pipeline.Insert(index, configuration);
                });
            });
    }

    public static IFusionGatewayBuilder InsertUseRequest(
        this IFusionGatewayBuilder builder,
        string before,
        RequestMiddlewareConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(before);
        ArgumentNullException.ThrowIfNull(configuration);

        return Configure(
            builder,
            options =>
            {
                options.PipelineModifiers.Add(pipeline =>
                {
                    var index = GetIndex(pipeline, before);

                    if (index == -1)
                    {
                        throw new InvalidOperationException($"The middleware with the key `{before}` was not found.");
                    }

                    pipeline.Insert(index, configuration);
                });
            });
    }

    private static int GetIndex(IList<RequestMiddlewareConfiguration> pipeline, string key)
    {
        for (var i = 0; i < pipeline.Count; i++)
        {
            if (pipeline[i].Key == key)
            {
                return i;
            }
        }

        return -1;
    }
}
