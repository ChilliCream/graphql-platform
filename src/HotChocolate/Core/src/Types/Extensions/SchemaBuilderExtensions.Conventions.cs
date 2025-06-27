using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

// ReSharper disable once CheckNamespace
namespace HotChocolate;

/// <summary>
/// Provides extension methods for the <see cref="ISchemaBuilder"/> interface.
/// </summary>
public static partial class SchemaBuilderExtensions
{
    /// <summary>
    /// Registers a convention with the schema builder,
    /// even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="type">
    /// The type of the convention to register.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention<T>(
        this ISchemaBuilder builder,
        Type type,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(type);

        return builder.AddConvention(typeof(T), type, scope);
    }

    /// <summary>
    /// Registers a convention with the schema builder,
    /// even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention<T>(
        this ISchemaBuilder builder,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.AddConvention(typeof(T), factory, scope);
    }

    /// <summary>
    /// Registers a convention with the schema builder, even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention(
        this ISchemaBuilder builder,
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
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.AddConvention(convention, _ => concreteConvention, scope);
    }

    /// <summary>
    /// Registers a convention with the schema builder, even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention(
        this ISchemaBuilder builder,
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
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        if (!typeof(IConvention).IsAssignableFrom(concreteConvention))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.AddConvention(
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
            scope);
    }

    /// <summary>
    /// Registers a convention with the schema builder, even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The convention to register.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention<T>(
        this ISchemaBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention
        => builder.AddConvention(typeof(T), convention, scope);

    /// <summary>
    /// Registers a convention with the schema builder, even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <typeparam name="TConvention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <typeparam name="TConcreteConvention">
    /// The concrete type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention<TConvention, TConcreteConvention>(
        this ISchemaBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : IConvention
        => builder.AddConvention(typeof(TConvention), typeof(TConcreteConvention), scope);

    /// <summary>
    /// Registers a convention with the schema builder, even if conventions of the same type and scope are already registered.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder AddConvention(
        this ISchemaBuilder builder,
        Type convention,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        var feature = builder.Features.GetOrSet<TypeSystemConventionFeature>();
        var key = new ConventionKey(convention, scope);
        var registration = new ConventionRegistration(key, factory);

        if (feature.Conventions.TryGetValue(new ConventionKey(convention, scope), out var registrations))
        {
            feature.Conventions = feature.Conventions.SetItem(key, registrations.Add(registration));
        }
        else
        {
            feature.Conventions = feature.Conventions.Add(key, [registration]);
        }

        return builder;
    }

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention(
        this ISchemaBuilder builder,
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
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        if (!typeof(IConvention).IsAssignableFrom(concreteConvention.GetType()))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.TryAddConvention(convention, _ => concreteConvention, scope);
    }

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="concreteConvention">
    /// The concrete type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the convention type is not supported.
    /// </exception>
    public static ISchemaBuilder TryAddConvention(
        this ISchemaBuilder builder,
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
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        return builder.TryAddConvention(
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
            scope);
    }

    /// <summary>
    /// Tries to register a convention with the schema builder
    /// if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention<T>(
        this ISchemaBuilder builder,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
        where T : IConvention
        => builder.TryAddConvention(typeof(T), factory, scope);

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="type">
    /// The type of the convention to register.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention<T>(
        this ISchemaBuilder builder,
        Type type,
        string? scope = null)
        where T : IConvention
        => builder.TryAddConvention(typeof(T), type, scope);

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The convention to register.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention<T>(
        this ISchemaBuilder builder,
        IConvention convention,
        string? scope = null)
        where T : IConvention
        => builder.TryAddConvention(typeof(T), convention, scope);

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <typeparam name="TConvention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </typeparam>
    /// <typeparam name="TConcreteConvention">
    /// The concrete type of the convention to register. Must implement <typeparamref name="TConvention"/>
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention<TConvention, TConcreteConvention>(
        this ISchemaBuilder builder,
        string? scope = null)
        where TConvention : IConvention
        where TConcreteConvention : class, TConvention
        => builder.TryAddConvention(typeof(TConvention), typeof(TConcreteConvention), scope);

    /// <summary>
    /// Tries to register a convention with the schema builder if no matching convention has been registered yet.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="convention">
    /// The type of the convention to register. Must implement <see cref="IConvention" />.
    /// </param>
    /// <param name="factory">
    /// A factory method used to create the convention instance from the <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="scope">
    /// An optional scope that distinguishes multiple conventions of the same type.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddConvention(
        this ISchemaBuilder builder,
        Type convention,
        Func<IServiceProvider, IConvention> factory,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);
        ArgumentNullException.ThrowIfNull(factory);

        if (!typeof(IConvention).IsAssignableFrom(convention))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilder_Convention_NotSupported,
                nameof(convention));
        }

        var feature = builder.Features.GetOrSet<TypeSystemConventionFeature>();

        if (!feature.Conventions.TryGetValue(new ConventionKey(convention, scope), out _))
        {
            var key = new ConventionKey(convention, scope);
            var registration = new ConventionRegistration(key, factory);
            feature.Conventions = feature.Conventions.Add(key, [registration]);
        }

        return builder;
    }

    /// <summary>
    /// Registers a schema directive with the schema builder.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="directive">
    /// The schema directive to register.
    /// </param>
    /// <returns>
    /// The schema builder instance.
    /// </returns>
    public static ISchemaBuilder TryAddSchemaDirective(
        this ISchemaBuilder builder,
        ISchemaDirective directive)
    {
        var feature = builder.Features.GetOrSet<TypeSystemFeature>();
        feature.SchemaDirectives.TryAdd(directive.Name, directive);
        return builder;
    }
}
