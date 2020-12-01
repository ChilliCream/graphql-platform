using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Configuration
{
    internal class DefaultOperationClientBuilder
        : IOperationClientBuilder
    {
        public DefaultOperationClientBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
