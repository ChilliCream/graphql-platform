using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers the JSON type converter that automatically converts any type to <see cref="System.Text.Json.JsonElement"/>
    /// by serializing it using <see cref="System.Text.Json.JsonSerializer"/>.
    /// This is useful when working with the <c>AnyType</c> scalar or when you need to serialize
    /// custom types to JSON for GraphQL output.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> to register the JSON type converter with.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> for chaining.
    /// </returns>
    /// <remarks>
    /// This converter uses expression compilation and reflection, which is not compatible with Native AOT.
    /// </remarks>
    [RequiresDynamicCode("The JSON type converter uses Expression.Compile which requires dynamic code generation.")]
    [RequiresUnreferencedCode("The JSON type converter uses reflection to access generic serialization methods.")]
    public static IRequestExecutorBuilder AddJsonTypeConverter(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.AddSingleton<IChangeTypeProvider, JsonElementTypeChangeProvider>();
        return builder;
    }

    /// <summary>
    /// Registers a custom type converter provider that can convert between types at runtime.
    /// Type converters are used when GraphQL needs to convert values between runtime types,
    /// such as converting input values to resolver parameter types or converting output values
    /// to the expected GraphQL field type.
    /// </summary>
    /// <typeparam name="T">
    /// The type converter provider implementation that implements <see cref="IChangeTypeProvider"/>.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> to register the type converter with.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeConverter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IChangeTypeProvider
    {
        builder.Services.AddSingleton<IChangeTypeProvider, T>();
        return builder;
    }

    /// <summary>
    /// Registers a custom type converter provider using a factory function.
    /// This overload allows the type converter to access services from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">
    /// The type converter provider implementation that implements <see cref="IChangeTypeProvider"/>.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> to register the type converter with.
    /// </param>
    /// <param name="factory">
    /// A factory function that creates the type converter instance using the service provider.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="builder"/> or <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeConverter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IChangeTypeProvider
    {
        builder.Services.AddSingleton<IChangeTypeProvider>(factory);
        return builder;
    }

    /// <summary>
    /// Registers a type converter that converts from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>
    /// using the provided delegate function.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type to convert from.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target type to convert to.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> to register the type converter with.
    /// </param>
    /// <param name="changeType">
    /// A function that performs the conversion from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="builder"/> or <paramref name="changeType"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.AddTypeConverter&lt;Foo, JsonElement&gt;(
    ///     from => JsonSerializer.SerializeToElement(from));
    /// </code>
    /// </example>
    public static IRequestExecutorBuilder AddTypeConverter<TSource, TTarget>(
        this IRequestExecutorBuilder builder,
        ChangeType<TSource, TTarget> changeType)
    {
        builder.Services.AddSingleton<IChangeTypeProvider>(
            new DelegateChangeTypeProvider<TSource, TTarget>(changeType));
        return builder;
    }

    /// <summary>
    /// Registers a type converter provider using a delegate that can handle multiple type conversions.
    /// The delegate receives the source and target types and returns a converter function if it can
    /// handle the conversion.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/> to register the type converter with.
    /// </param>
    /// <param name="changeType">
    /// A delegate that examines the source and target types and returns a converter function if applicable.
    /// </param>
    /// <returns>
    /// The <see cref="IRequestExecutorBuilder"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="builder"/> or <paramref name="changeType"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeConverter(
        this IRequestExecutorBuilder builder,
        ChangeTypeProvider changeType)
    {
        builder.Services.AddSingleton<IChangeTypeProvider>(
            new DelegateChangeTypeProvider(changeType));
        return builder;
    }

    /// <summary>
    /// Registers the JSON type converter that automatically converts any type to <see cref="System.Text.Json.JsonElement"/>
    /// by serializing it using <see cref="System.Text.Json.JsonSerializer"/>.
    /// This is useful when working with the <c>AnyType</c> scalar or when you need to serialize
    /// custom types to JSON for GraphQL output.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to register the JSON type converter with.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for chaining.
    /// </returns>
    /// <remarks>
    /// This converter uses expression compilation and reflection, which is not compatible with Native AOT.
    /// </remarks>
    [RequiresDynamicCode("The JSON type converter uses Expression.Compile which requires dynamic code generation.")]
    [RequiresUnreferencedCode("The JSON type converter uses reflection to access generic serialization methods.")]
    public static IServiceCollection AddJsonTypeConverter(
        this IServiceCollection services)
    {
        return services.AddSingleton<IChangeTypeProvider, JsonElementTypeChangeProvider>();
    }

    /// <summary>
    /// Registers a custom type converter provider that can convert between types at runtime.
    /// Type converters are used when GraphQL needs to convert values between runtime types,
    /// such as converting input values to resolver parameter types or converting output values
    /// to the expected GraphQL field type.
    /// </summary>
    /// <typeparam name="T">
    /// The type converter provider implementation that implements <see cref="IChangeTypeProvider"/>.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to register the type converter with.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTypeConverter<T>(
        this IServiceCollection services)
        where T : class, IChangeTypeProvider
    {
        return services.AddSingleton<IChangeTypeProvider, T>();
    }

    /// <summary>
    /// Registers a custom type converter provider using a factory function.
    /// This overload allows the type converter to access services from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">
    /// The type converter provider implementation that implements <see cref="IChangeTypeProvider"/>.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to register the type converter with.
    /// </param>
    /// <param name="factory">
    /// A factory function that creates the type converter instance using the service provider.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services"/> or <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTypeConverter<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IChangeTypeProvider
    {
        return services.AddSingleton<IChangeTypeProvider>(factory);
    }

    /// <summary>
    /// Registers a type converter that converts from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>
    /// using the provided delegate function.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type to convert from.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target type to convert to.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to register the type converter with.
    /// </param>
    /// <param name="changeType">
    /// A function that performs the conversion from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services"/> or <paramref name="changeType"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddTypeConverter&lt;Foo, JsonElement&gt;(
    ///     from => JsonSerializer.SerializeToElement(from));
    /// </code>
    /// </example>
    public static IServiceCollection AddTypeConverter<TSource, TTarget>(
        this IServiceCollection services,
        ChangeType<TSource, TTarget> changeType)
    {
        return services.AddSingleton<IChangeTypeProvider>(
            new DelegateChangeTypeProvider<TSource, TTarget>(changeType));
    }

    /// <summary>
    /// Registers a type converter provider using a delegate that can handle multiple type conversions.
    /// The delegate receives the source and target types and returns a converter function if it can
    /// handle the conversion.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to register the type converter with.
    /// </param>
    /// <param name="changeType">
    /// A delegate that examines the source and target types and returns a converter function if applicable.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="services"/> or <paramref name="changeType"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddTypeConverter(
        this IServiceCollection services,
        ChangeTypeProvider changeType)
    {
        return services.AddSingleton<IChangeTypeProvider>(
            new DelegateChangeTypeProvider(changeType));
    }

    private sealed class DelegateChangeTypeProvider(
        ChangeTypeProvider changeTypeProvider)
        : IChangeTypeProvider
    {
        private readonly ChangeTypeProvider _changeTypeProvider = changeTypeProvider;

        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            [NotNullWhen(true)] out ChangeType? converter)
            => _changeTypeProvider(source, target, out converter);
    }

    private sealed class DelegateChangeTypeProvider<TSource, TTarget>(
        ChangeType<TSource, TTarget> changeType)
        : IChangeTypeProvider
    {
        private readonly ChangeType<TSource, TTarget> _changeType = changeType;

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
