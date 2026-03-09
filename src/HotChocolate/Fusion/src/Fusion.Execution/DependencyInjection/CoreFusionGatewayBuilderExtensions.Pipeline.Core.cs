using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        Func<RequestDelegate, RequestDelegate> middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return FusionSetupUtilities.Configure(
                builder,
                options => options.PipelineModifiers.Add(
                    pipeline => pipeline.Add(
                        new RequestMiddlewareConfiguration((_, n) => middleware(n), key))));
        }

        if (!allowMultiple && key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(key));
        }

        return FusionSetupUtilities.Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration((_, n) => middleware(n), key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddleware middleware,
        string? key = null,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return FusionSetupUtilities.Configure(
                builder,
                options => options.PipelineModifiers.Add(
                    pipeline => pipeline.Add(
                        new RequestMiddlewareConfiguration(middleware, key))));
        }

        if (!allowMultiple && key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(key));
        }

        return FusionSetupUtilities.Configure(
            builder,
            options =>
            {
                var configuration = new RequestMiddlewareConfiguration(middleware, key);

                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
                });
            });
    }

    public static IFusionGatewayBuilder UseRequest(
        this IFusionGatewayBuilder builder,
        RequestMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null,
        bool allowMultiple = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        if (before is not null && after is not null)
        {
            throw new ArgumentException(
                "Only one of 'before' or 'after' can be specified at the same time.");
        }

        if (before is null && after is null)
        {
            return FusionSetupUtilities.Configure(
                builder,
                options => options.PipelineModifiers.Add(
                    pipeline => pipeline.Add(configuration)));
        }

        if (!allowMultiple && configuration.Key is null)
        {
            throw new ArgumentException(
                "The key must be set if allowMultiple is false.",
                nameof(configuration));
        }

        return FusionSetupUtilities.Configure(
            builder,
            options =>
            {
                options.PipelineModifiers.Add(pipeline =>
                {
                    if (!allowMultiple && GetIndex(pipeline, configuration.Key!) != -1)
                    {
                        return;
                    }

                    var anchor = (before ?? after)!;
                    var index = GetIndex(pipeline, anchor);

                    if (index == -1)
                    {
                        throw new InvalidOperationException(
                            $"The middleware with the key `{anchor}` was not found.");
                    }

                    pipeline.Insert(before is not null ? index : index + 1, configuration);
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
