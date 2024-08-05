using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddTypeConverter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IChangeTypeProvider
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IChangeTypeProvider, T>();
        return builder;
    }

    public static IRequestExecutorBuilder AddTypeConverter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IChangeTypeProvider
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IChangeTypeProvider>(factory);
        return builder;
    }

    public static IRequestExecutorBuilder AddTypeConverter<TSource, TTarget>(
        this IRequestExecutorBuilder builder,
        ChangeType<TSource, TTarget> changeType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IChangeTypeProvider>(
            sp => new DelegateChangeTypeProvider<TSource, TTarget>(changeType));
        return builder;
    }

    public static IRequestExecutorBuilder AddTypeConverter(
        this IRequestExecutorBuilder builder,
        ChangeTypeProvider changeType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IChangeTypeProvider>(
            sp => new DelegateChangeTypeProvider(changeType));
        return builder;
    }

    public static IServiceCollection AddTypeConverter<T>(
        this IServiceCollection services)
        where T : class, IChangeTypeProvider
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddSingleton<IChangeTypeProvider, T>();
    }

    public static IServiceCollection AddTypeConverter<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IChangeTypeProvider
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddSingleton<IChangeTypeProvider>(factory);
    }

    public static IServiceCollection AddTypeConverter<TSource, TTarget>(
        this IServiceCollection services,
        ChangeType<TSource, TTarget> changeType)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddSingleton<IChangeTypeProvider>(
            sp => new DelegateChangeTypeProvider<TSource, TTarget>(changeType));
    }

    public static IServiceCollection AddTypeConverter(
        this IServiceCollection services,
        ChangeTypeProvider changeType)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddSingleton<IChangeTypeProvider>(
            sp => new DelegateChangeTypeProvider(changeType));
    }

    private sealed class DelegateChangeTypeProvider : IChangeTypeProvider
    {
        private readonly ChangeTypeProvider _changeTypeProvider;

        public DelegateChangeTypeProvider(ChangeTypeProvider changeTypeProvider)
        {
            _changeTypeProvider = changeTypeProvider;
        }

        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            [NotNullWhen(true)] out ChangeType? converter)
        {
            return _changeTypeProvider(source, target, out converter);
        }
    }

    private sealed class DelegateChangeTypeProvider<TSource, TTarget> : IChangeTypeProvider
    {
        private readonly ChangeType<TSource, TTarget> _changeType;

        public DelegateChangeTypeProvider(
            ChangeType<TSource, TTarget> changeType)
        {
            _changeType = changeType;
        }

        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            [NotNullWhen(true)] out ChangeType? converter)
        {
            if (source == typeof(TSource) && target == typeof(TTarget))
            {
                converter = input =>
                {
                    if (input is null)
                    {
                        return default(TTarget);
                    }

                    return _changeType((TSource)input);
                };
                return true;
            }

            converter = null;
            return false;
        }
    }
}
