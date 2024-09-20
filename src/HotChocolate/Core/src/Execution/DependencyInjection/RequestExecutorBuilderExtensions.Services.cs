using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a initializer to copy state between a request scoped service instance and
    /// a resolver scoped service instance.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="initializer">
    /// The initializer that is used to initialize the service scope.
    /// </param>
    /// <typeparam name="TService">
    /// The type of the service that shall be initialized.
    /// </typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddScopedServiceInitializer<TService>(
        this IRequestExecutorBuilder builder,
        Action<TService, TService> initializer)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (initializer == null)
        {
            throw new ArgumentNullException(nameof(initializer));
        }

        builder.Services.AddSingleton<IServiceScopeInitializer>(new DelegateServiceInitializer<TService>(initializer));
        return builder;
    }
}

file sealed class DelegateServiceInitializer<TService>(
    Action<TService, TService> initializer)
    : ServiceInitializer<TService>
{
    private readonly Action<TService, TService> _initializer = initializer ??
        throw new ArgumentNullException(nameof(initializer));

    protected override void Initialize(TService requestScopeService, TService resolverScopeService)
        => _initializer(requestScopeService, resolverScopeService);
}
