using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// Provides extension methods on <see cref="IServiceCollection"/> for registering the message
/// bus's <see cref="MochaMessageBusResourceSource"/> contributor and the built-in resource
/// definitions.
/// </summary>
public static class MochaMessageBusResourcesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the message bus <see cref="MochaResourceSource"/> contributor and the catalog
    /// of built-in resource kinds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolves <see cref="MessagingRuntime"/> via DI and instantiates a
    /// <see cref="MochaMessageBusResourceSource"/> bound to it. The source is added as a
    /// contributor to the composite installed by <see cref="MochaResourcesServiceCollectionExtensions.AddMochaResources"/>;
    /// callers that haven't yet wired up the composite get it implicitly because this method calls
    /// <see cref="MochaResourcesServiceCollectionExtensions.AddMochaResources"/> as well.
    /// </para>
    /// <para>
    /// Definitions are advisory only — they let UIs render kind pickers and tooltips. Adding new
    /// resource kinds is a question of registering more <see cref="MochaResourceDefinition"/>
    /// entries; the runtime never validates against them.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMochaMessageBusResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMochaResources();

        services.TryAddSingleton(static sp =>
            new MochaMessageBusResourceSource((MessagingRuntime)sp.GetRequiredService<IMessagingRuntime>()));

        services.AddSingleton<IMochaResourceSourceContributor>(
            static sp => new MochaMessageBusResourceSourceContributor(sp.GetRequiredService<MochaMessageBusResourceSource>()));

        AddBuiltInDefinitions(services);

        return services;
    }

    private sealed class MochaMessageBusResourceSourceContributor : IMochaResourceSourceContributor
    {
        public MochaMessageBusResourceSourceContributor(MochaMessageBusResourceSource source)
        {
            Source = source;
        }

        public MochaResourceSource Source { get; }
    }

    private static void AddBuiltInDefinitions(IServiceCollection services)
    {
        services.AddSingleton(new MochaResourceDefinition("mocha.service", "Service"));
        services.AddSingleton(new MochaResourceDefinition("mocha.message_type", "Message Type"));
        services.AddSingleton(new MochaResourceDefinition("mocha.handler", "Handler"));
        services.AddSingleton(new MochaResourceDefinition("mocha.inbound_route", "Inbound Route"));
        services.AddSingleton(new MochaResourceDefinition("mocha.outbound_route", "Outbound Route"));
        services.AddSingleton(new MochaResourceDefinition("mocha.transport", "Transport"));
        services.AddSingleton(new MochaResourceDefinition("mocha.receive_endpoint", "Receive Endpoint"));
        services.AddSingleton(new MochaResourceDefinition("mocha.dispatch_endpoint", "Dispatch Endpoint"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga", "Saga"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga.state", "Saga State"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga.transition", "Saga Transition"));
        services.AddSingleton(new MochaResourceDefinition("mocha.queue", "Queue"));
        services.AddSingleton(new MochaResourceDefinition("mocha.exchange", "Exchange"));
        services.AddSingleton(new MochaResourceDefinition("mocha.topic", "Topic"));
        services.AddSingleton(new MochaResourceDefinition("mocha.binding", "Binding"));
    }
}
