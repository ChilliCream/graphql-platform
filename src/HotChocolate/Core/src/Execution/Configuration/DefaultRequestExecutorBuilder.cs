using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    internal class DefaultRequestExecutorBuilder : IRequestExecutorBuilder
    {
        public DefaultRequestExecutorBuilder(
            IServiceCollection services,
            NameString name)
        {
            Services = services;
            Name = name;
        }

        public NameString Name { get; }

        public IServiceCollection Services { get; }
    }
}