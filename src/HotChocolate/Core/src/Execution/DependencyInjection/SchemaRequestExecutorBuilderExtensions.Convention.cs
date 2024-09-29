using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Properties;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Execution.ThrowHelper;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        CreateConvention factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchema(b => b.AddConvention(convention, factory, scope));
    }

    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        Type type,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(type);

        return builder.ConfigureSchema(b => b.AddConvention(typeof(T), type, scope));
    }

    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        CreateConvention conventionFactory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(
            b => b.AddConvention(typeof(T), conventionFactory, scope));
    }

    public static IRequestExecutorBuilder AddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        IConvention concreteConvention,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(concreteConvention);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.ConfigureSchema(
            b => b.AddConvention(convention, _ => concreteConvention, scope));
    }

    public static IRequestExecutorBuilder AddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        Type concreteConvention,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(concreteConvention);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.ConfigureSchema(
            b => b.AddConvention(
                convention,
                s =>
                {
                    try
                    {
                        return (IConvention)ActivatorUtilities.CreateInstance(s, concreteConvention);
                    }
                    catch
                    {
                        throw Convention_UnableToCreateConvention(concreteConvention);
                    }
                },
                scope));
    }

    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention =>
        builder.ConfigureSchema(b => b.AddConvention(typeof(T), convention, scope));

    public static IRequestExecutorBuilder AddConvention<TConvention, TConcreteConvention>(
        this IRequestExecutorBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : IConvention =>
        builder.ConfigureSchema(
            b => b.AddConvention(typeof(TConvention), typeof(TConcreteConvention), scope));

    public static IRequestExecutorBuilder TryAddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        CreateConvention factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchema(b => b.TryAddConvention(convention, factory, scope));
    }

    public static IRequestExecutorBuilder TryAddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        IConvention concreteConvention,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(concreteConvention);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.ConfigureSchema(
            b => b.TryAddConvention(convention, _ => concreteConvention, scope));
    }

    public static IRequestExecutorBuilder TryAddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        Type concreteConvention,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(concreteConvention);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
        {
            throw new ArgumentException(
                Resources.RequestExecutorBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.ConfigureSchema(
            b => b.TryAddConvention(
                convention,
                s =>
                {
                    try
                    {
                        return (IConvention)ActivatorUtilities.CreateInstance(s, concreteConvention);
                    }
                    catch
                    {
                        throw Convention_UnableToCreateConvention(concreteConvention);
                    }
                },
                scope));
    }

    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        CreateConvention conventionFactory,
        string? scope = null)
        where T : IConvention =>
        builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), conventionFactory, scope));

    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        Type type,
        string? scope = null)
        where T : IConvention =>
        builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), type, scope));

    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention =>
        builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), convention, scope));

    public static IRequestExecutorBuilder TryAddConvention<TConvention, TConcreteConvention>(
        this IRequestExecutorBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : class, TConvention =>
        builder.ConfigureSchema(
            b => b.TryAddConvention(typeof(TConvention), typeof(TConcreteConvention), scope));
}
