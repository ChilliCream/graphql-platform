using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

internal sealed class MessageBusHostBuilder(IServiceCollection services, string name) : IMessageBusHostBuilder
{
    public string Name { get; } = name;

    public IServiceCollection Services { get; } = services;
}
