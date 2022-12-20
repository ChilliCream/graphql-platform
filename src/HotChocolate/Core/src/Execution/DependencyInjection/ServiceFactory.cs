using System;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.DependencyInjection;

internal sealed class ServiceFactory<T> : IFactory<T>
{
    private readonly IServiceProvider _services;

    public ServiceFactory(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public T Create() => _services.GetRequiredService<T>();
}
