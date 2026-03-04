using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Execution.ThrowHelper;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention
    /// instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchema(b => b.AddConvention(convention, factory, scope));
    }

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="type">
    /// The convention type.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        Type type,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(type);

        return builder.ConfigureSchema(b => b.AddConvention(typeof(T), type, scope));
    }

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <typeparam name="T">
    /// The convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="conventionFactory">
    /// A factory method used to create the convention
    /// instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IConvention> conventionFactory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(
            b => b.AddConvention(typeof(T), conventionFactory, scope));
    }

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
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

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete implementation convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the convention or concrete convention is not assignable
    /// from <see cref="IConvention" />.
    /// </exception>
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

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <typeparam name="T">
    /// The convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddConvention<T>(
        this IRequestExecutorBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);

        return builder.ConfigureSchema(b => b.AddConvention(typeof(T), convention, scope));
    }

    /// <summary>
    /// Registers a convention with the GraphQL schema,
    /// even if conventions of the same type and scope
    /// are already registered.
    /// </summary>
    /// <typeparam name="TConvention">
    /// The convention type.
    /// </typeparam>
    /// <typeparam name="TConcreteConvention">
    /// The concrete convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddConvention<TConvention, TConcreteConvention>(
        this IRequestExecutorBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : IConvention
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(
            b => b.AddConvention(typeof(TConvention), typeof(TConcreteConvention), scope));
    }

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention
    /// instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder TryAddConvention(
        this IRequestExecutorBuilder builder,
        Type convention,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchema(b => b.TryAddConvention(convention, factory, scope));
    }

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete implementation convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the convention or concrete convention is not assignable
    /// from <see cref="IConvention" />.
    /// </exception>
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

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete implementation convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the convention or concrete convention is not assignable
    /// from <see cref="IConvention" />.
    /// </exception>
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

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="conventionFactory">
    /// A factory method used to create the convention
    /// instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IConvention> conventionFactory,
        string? scope = null)
        where T : IConvention
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(conventionFactory);

        return builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), conventionFactory, scope));
    }

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="type">
    /// The convention type.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        Type type,
        string? scope = null)
        where T : IConvention
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(type);

        return builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), type, scope));
    }

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="convention">
    /// The convention.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder TryAddConvention<T>(
        this IRequestExecutorBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);

        return builder.ConfigureSchema(b => b.TryAddConvention(typeof(T), convention, scope));
    }

    /// <summary>
    /// Tries to register a convention with the GraphQL schema
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="TConvention">
    /// The convention type.
    /// </typeparam>
    /// <typeparam name="TConcreteConvention">
    /// The concrete convention type.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions
    /// of the same type.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder TryAddConvention<TConvention, TConcreteConvention>(
        this IRequestExecutorBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : class, TConvention
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(
            b => b.TryAddConvention(
                typeof(TConvention),
                typeof(TConcreteConvention),
                scope));
    }
}
