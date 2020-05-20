using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    internal class DefaultRequestExecutorBuilder : IRequestExecutorBuilder
    {
        public DefaultRequestExecutorBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name; 
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}