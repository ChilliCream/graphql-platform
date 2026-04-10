using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

internal sealed class MediatorHostBuilder(IServiceCollection services, string name) : IMediatorHostBuilder
{
    public string Name { get; } = name;

    public IServiceCollection Services { get; } = services;

    public MediatorOptions Options { get; } = new();
}
